using FluentResults;

using LeoHR.Server.Features.OnboardingTemplates.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeoHR.Server.Features.OnboardingTemplates;

public class OnboardingTemplatesService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeApiClient employeeApi,
    IOnboardingTypeArchive typeArchive,
    ILogger<OnboardingTemplatesService> logger) : IOnboardingTemplatesService
{
    private const string ResourceName = "Onboarding task template";

    public async Task<List<OnboardingTemplateDto>> GetAll()
    {
        List<OnboardingTaskTemplateEntity> templates = await QueryWithChildren()
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .ToListAsync();

        Dictionary<string, string> editorNames = await GetEditorNames(templates);
        return [.. templates.Select(t => MapToDto(t, editorNames))];
    }

    public async Task<Result<OnboardingTemplateDto>> GetById(int id)
    {
        OnboardingTaskTemplateEntity? template = await QueryWithChildren()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        Dictionary<string, string> editorNames = await GetEditorNames([template]);
        return Result.Ok(MapToDto(template, editorNames));
    }

    public async Task<Result<OnboardingTemplateDto>> Create(SaveTemplateRequest request)
    {
        Result validation = Validate(request);
        if (validation.IsFailed)
        {
            return validation.ToResult<OnboardingTemplateDto>();
        }

        if (!await MilestoneExists(request.MilestoneId))
        {
            return Result.Fail(new BusinessRuleError("Unknown milestone.", nameof(SaveTemplateRequest.MilestoneId)));
        }

        if (await TitleTakenInPhase(request.Title, request.Phase, excludeTemplateId: null))
        {
            return Result.Fail(DuplicateTitle(request.Title));
        }

        var template = new OnboardingTaskTemplateEntity { IsActive = true };
        Result applied = ApplyRequest(template, request);
        if (applied.IsFailed)
        {
            return applied.ToResult<OnboardingTemplateDto>();
        }

        db.OnboardingTaskTemplates.Add(template);
        await db.SaveChangesAsync();

        return await GetById(template.Id);
    }

    public async Task<Result<OnboardingTemplateDto>> Update(int id, SaveTemplateRequest request)
    {
        Result validation = Validate(request);
        if (validation.IsFailed)
        {
            return validation.ToResult<OnboardingTemplateDto>();
        }

        if (!await MilestoneExists(request.MilestoneId))
        {
            return Result.Fail(new BusinessRuleError("Unknown milestone.", nameof(SaveTemplateRequest.MilestoneId)));
        }

        OnboardingTaskTemplateEntity? template = await QueryWithChildren().FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        // Titles are unique among active templates only; SetStatus re-checks on reactivation.
        if (template.IsActive && await TitleTakenInPhase(request.Title, request.Phase, excludeTemplateId: id))
        {
            return Result.Fail(DuplicateTitle(request.Title));
        }

        Result applied = ApplyRequest(template, request);
        if (applied.IsFailed)
        {
            return applied.ToResult<OnboardingTemplateDto>();
        }

        // Child-only edits leave the template Unchanged, and then SaveChangesAsync wouldn't stamp UpdatedAt/UpdatedBy.
        db.Entry(template).State = EntityState.Modified;
        await db.SaveChangesAsync();

        return await GetById(id);
    }

    public async Task<Result<OnboardingTemplateDto>> SetStatus(int id, bool isActive)
    {
        OnboardingTaskTemplateEntity? template = await db.OnboardingTaskTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        // Titles are unique among active templates only, so reactivating can collide with a newer one.
        if (isActive && !template.IsActive && await TitleTakenInPhase(template.Title, template.Phase, excludeTemplateId: id))
        {
            return Result.Fail(new BusinessRuleError(
                $"Another active template in this phase is already called '{template.Title}'. Rename it before reactivating.",
                nameof(OnboardingTaskTemplateEntity.Title)));
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

        // Archive before the flag flips: the types are found while this template still counts.
        if (!isActive && template.IsActive)
        {
            await typeArchive.ArchiveTypesLeftEmptyBy(id);
        }

        template.IsActive = isActive;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetById(id);
    }

    public async Task<Result> Delete(int id)
    {
        OnboardingTaskTemplateEntity? template = await db.OnboardingTaskTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

        // The delete cascades to the types' selections, so find the types it would empty first.
        await typeArchive.ArchiveTypesLeftEmptyBy(id);

        db.OnboardingTaskTemplates.Remove(template);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Result.Ok();
    }

    public async Task<Result<List<AffectedTypeDto>>> GetArchiveImpact(int id)
    {
        if (!await db.OnboardingTaskTemplates.AnyAsync(t => t.Id == id))
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        return Result.Ok(await typeArchive.GetTypesLeftEmptyBy(id));
    }

    private IQueryable<OnboardingTaskTemplateEntity> QueryWithChildren()
    {
        return db.OnboardingTaskTemplates
            .Include(t => t.SuggestedLevels)
            .Include(t => t.Fields)
                .ThenInclude(f => f.ChecklistItems);
    }

    public async Task<List<MilestoneOptionDto>> GetMilestones()
    {
        return await db.OnboardingMilestones
            .AsNoTracking()
            .OrderBy(m => m.SortOrder)
            .Select(m => new MilestoneOptionDto(m.Id, m.Name))
            .ToListAsync();
    }

    private Task<bool> MilestoneExists(int milestoneId)
    {
        return db.OnboardingMilestones.AnyAsync(m => m.Id == milestoneId);
    }

    private Task<bool> TitleTakenInPhase(string title, OnboardingPhase phase, int? excludeTemplateId)
    {
        string normalized = title.Trim().ToLower();

        return db.OnboardingTaskTemplates
            .Where(t => t.IsActive && t.Phase == phase && t.Id != excludeTemplateId)
            .AnyAsync(t => t.Title.ToLower() == normalized);
    }

    private static DuplicateResourceError DuplicateTitle(string title)
    {
        return new DuplicateResourceError(ResourceName, title.Trim(), nameof(SaveTemplateRequest.Title));
    }

    private async Task<Dictionary<string, string>> GetEditorNames(IEnumerable<OnboardingTaskTemplateEntity> templates)
    {
        List<string> editorIds = [.. templates.Select(t => t.UpdatedBy).OfType<string>().Distinct()];
        if (editorIds.Count == 0)
        {
            return [];
        }

        try
        {
            List<EmployeeRef> editors = await employeeLookup.GetByIdsAsync(editorIds);
            return editors.ToDictionary(e => e.Id, e => $"{e.FirstName} {e.LastName}".Trim());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not resolve template editor names; showing templates without them");
            return [];
        }
    }

    private static Result Validate(SaveTemplateRequest request)
    {
        if (request.ResponsibleRoleKey is not null && FindRole(request.ResponsibleRoleKey) is null)
        {
            return Result.Fail(new BusinessRuleError(
                $"Unknown responsible role '{request.ResponsibleRoleKey}'.",
                nameof(SaveTemplateRequest.ResponsibleRoleKey)));
        }

        if (request.AppliesTo is TemplateAppliesTo.Optional && (request.SuggestedFor is null || request.SuggestedFor.Count == 0))
        {
            return Result.Fail(new BusinessRuleError(
                "An optional template needs at least one suggested level.",
                nameof(SaveTemplateRequest.SuggestedFor)));
        }

        foreach (SaveFieldInput field in request.Fields ?? [])
        {
            if (field.FieldType is TemplateFieldType.Checklist && (field.ChecklistItems is null || field.ChecklistItems.Count == 0))
            {
                return Result.Fail(new BusinessRuleError(
                    $"Checklist field '{field.Label}' needs at least one item.",
                    nameof(SaveFieldInput.ChecklistItems)));
            }
        }

        return Result.Ok();
    }

    private static Result ApplyRequest(OnboardingTaskTemplateEntity template, SaveTemplateRequest request)
    {
        template.Title = request.Title.Trim();
        template.Phase = request.Phase;
        template.MilestoneId = request.MilestoneId;
        template.ResponsibleRoleKey = request.ResponsibleRoleKey;
        template.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        template.DueOffsetDays = request.DueOffsetDays;
        template.DueDirection = request.DueDirection;
        template.DueAnchor = request.DueAnchor;
        template.AppliesTo = request.AppliesTo;

        List<Seniority> levels = request.AppliesTo is TemplateAppliesTo.Optional
            ? [.. (request.SuggestedFor ?? []).Distinct()]
            : [];

        template.SuggestedLevels.Clear();
        foreach (Seniority level in levels)
        {
            template.SuggestedLevels.Add(new OnboardingTaskTemplateSuggestedLevelEntity { Level = level });
        }

        return ApplyFields(template, request.Fields ?? []);
    }

    public async Task<List<ResponsibleRoleOptionDto>> GetPersonRestrictionOptions()
    {
        IReadOnlyList<ApiEmployee> employees = await employeeApi.GetAllAsync();
        return [.. PersonRestrictions.Positions(employees).Select(p => new ResponsibleRoleOptionDto(p, p))];
    }

    private static OnboardingRoleDefinition? FindRole(string key)
    {
        return OnboardingRoles.Find(key);
    }

    private static Result ApplyFields(OnboardingTaskTemplateEntity template, List<SaveFieldInput> inputs)
    {
        var existing = template.Fields.ToDictionary(f => f.Id);
        HashSet<int> kept = [];

        for (int i = 0; i < inputs.Count; i++)
        {
            SaveFieldInput input = inputs[i];
            OnboardingTaskTemplateFieldEntity field;
            if (input.Id is int id)
            {
                if (!existing.TryGetValue(id, out OnboardingTaskTemplateFieldEntity? found) || !kept.Add(id))
                {
                    return Result.Fail(new BusinessRuleError($"Field {id} does not belong to this template.", nameof(SaveFieldInput.Id)));
                }

                field = found;
            }
            else
            {
                field = new OnboardingTaskTemplateFieldEntity();
                template.Fields.Add(field);
            }

            field.SortOrder = i;
            field.FieldType = input.FieldType;
            field.Label = input.Label.Trim();
            field.Hint = string.IsNullOrWhiteSpace(input.Hint) ? null : input.Hint.Trim();
            field.PersonRestrictionKey = input.FieldType is TemplateFieldType.EmployeePicker && !string.IsNullOrWhiteSpace(input.PersonRestrictionKey)
                ? input.PersonRestrictionKey.Trim()
                : null;

            Result items = ApplyChecklistItems(field, input.FieldType is TemplateFieldType.Checklist ? input.ChecklistItems ?? [] : []);
            if (items.IsFailed)
            {
                return items;
            }
        }

        foreach (OnboardingTaskTemplateFieldEntity removed in template.Fields.Where(f => f.Id != 0 && !kept.Contains(f.Id)).ToList())
        {
            template.Fields.Remove(removed);
        }

        return Result.Ok();
    }

    private static Result ApplyChecklistItems(OnboardingTaskTemplateFieldEntity field, List<SaveChecklistItemInput> inputs)
    {
        var existing = field.ChecklistItems.ToDictionary(i => i.Id);
        HashSet<int> kept = [];

        for (int i = 0; i < inputs.Count; i++)
        {
            SaveChecklistItemInput input = inputs[i];
            OnboardingTaskTemplateChecklistItemEntity item;
            if (input.Id is int id)
            {
                if (!existing.TryGetValue(id, out OnboardingTaskTemplateChecklistItemEntity? found) || !kept.Add(id))
                {
                    return Result.Fail(new BusinessRuleError($"Checklist item {id} does not belong to this field.", nameof(SaveChecklistItemInput.Id)));
                }

                item = found;
            }
            else
            {
                item = new OnboardingTaskTemplateChecklistItemEntity();
                field.ChecklistItems.Add(item);
            }

            item.SortOrder = i;
            item.Text = input.Text.Trim();
        }

        foreach (OnboardingTaskTemplateChecklistItemEntity removed in field.ChecklistItems.Where(i => i.Id != 0 && !kept.Contains(i.Id)).ToList())
        {
            field.ChecklistItems.Remove(removed);
        }

        return Result.Ok();
    }

    private static OnboardingTemplateDto MapToDto(OnboardingTaskTemplateEntity t, Dictionary<string, string> editorNames)
    {
        return new OnboardingTemplateDto(
            Id: t.Id,
            Title: t.Title,
            Phase: t.Phase,
            MilestoneId: t.MilestoneId,
            ResponsibleRoleKey: t.ResponsibleRoleKey,
            ResponsibleRoleName: t.ResponsibleRoleKey is null ? null : FindRole(t.ResponsibleRoleKey)?.ExactTitle,
            Description: t.Description,
            DueOffsetDays: t.DueOffsetDays,
            DueDirection: t.DueDirection,
            DueAnchor: t.DueAnchor,
            AppliesTo: t.AppliesTo,
            SuggestedFor: [.. t.SuggestedLevels.Select(l => l.Level).OrderBy(l => l)],
            IsActive: t.IsActive,
            Fields: [.. t.Fields.OrderBy(f => f.SortOrder).Select(MapFieldToDto)],
            UpdatedAt: t.UpdatedAt,
            UpdatedByName: t.UpdatedBy is null ? null : editorNames.GetValueOrDefault(t.UpdatedBy)
        );
    }

    private static TemplateFieldDto MapFieldToDto(OnboardingTaskTemplateFieldEntity f)
    {
        return new TemplateFieldDto(
            Id: f.Id,
            FieldType: f.FieldType,
            Label: f.Label,
            Hint: f.Hint,
            PersonRestrictionKey: f.PersonRestrictionKey,
            ChecklistItems: [.. f.ChecklistItems.OrderBy(i => i.SortOrder).Select(i => new TemplateChecklistItemDto(i.Id, i.Text))]
        );
    }
}
