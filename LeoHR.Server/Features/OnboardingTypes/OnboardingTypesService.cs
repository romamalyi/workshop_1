using System.Text.RegularExpressions;

using FluentResults;

using LeoHR.Server.Features.OnboardingTypes.Dto;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Onboarding;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingTypes;

public partial class OnboardingTypesService(LeoHrDbContext db) : IOnboardingTypesService
{
    private const string ResourceName = "Onboarding type";
    private const string ActiveNeedsTemplateMessage = "A type needs at least one enabled template to be active.";
    private const string TypeNeedsTemplateMessage = "A type needs at least one enabled template.";
    private const string ArchivedNotDuplicableMessage = "An archived type can't be duplicated.";

    public async Task<OnboardingTypesDto> GetAll()
    {
        int libraryTemplateCount = await db.OnboardingTaskTemplates.CountAsync(t => t.IsActive);

        List<OnboardingTypeDto> types = await ToDto(db.OnboardingTypes
                .OrderBy(t => t.IsArchived)
                .ThenByDescending(t => t.UpdatedAt))
            .ToListAsync();

        return new OnboardingTypesDto(libraryTemplateCount, types);
    }

    public async Task<List<TypeLibraryTemplateDto>> GetLibrary()
    {
        List<OnboardingTaskTemplateEntity> templates = await db.OnboardingTaskTemplates
            .AsNoTracking()
            .Include(t => t.SuggestedLevels)
            .Include(t => t.Fields)
                .ThenInclude(f => f.ChecklistItems)
            .OrderBy(t => t.Id)
            .ToListAsync();

        return [.. templates.Select(t => new TypeLibraryTemplateDto(
            Id: t.Id,
            Title: t.Title,
            Description: t.Description,
            Phase: t.Phase,
            ResponsibleRoleName: OnboardingRoles.Find(t.ResponsibleRoleKey)?.ExactTitle,
            DueOffsetDays: t.DueOffsetDays,
            DueDirection: t.DueDirection,
            DueAnchor: t.DueAnchor,
            AppliesTo: t.AppliesTo,
            SuggestedFor: [.. t.SuggestedLevels.Select(l => l.Level)],
            IsActive: t.IsActive,
            Fields: [.. t.Fields.OrderBy(f => f.SortOrder).Select(f => new TypeLibraryFieldDto(f.FieldType, f.ChecklistItems.Count))]))];
    }

    public async Task<Result<OnboardingTypeDetailDto>> GetById(int id)
    {
        OnboardingTypeEntity? type = await db.OnboardingTypes
            .AsNoTracking()
            .Include(t => t.Templates)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        return Result.Ok(new OnboardingTypeDetailDto(
            type.Id,
            type.Name,
            type.Description,
            type.IsArchived,
            [.. type.Templates.OrderBy(t => t.TemplateId).Select(t => new TypeTemplateSelectionDto(t.TemplateId, t.IsEnabled))]));
    }

    public async Task<Result<OnboardingTypeDto>> Create(SaveTypeRequest request)
    {
        Result validation = await Validate(request, exceptId: null);
        if (validation.IsFailed)
        {
            return validation.ToResult<OnboardingTypeDto>();
        }

        var type = new OnboardingTypeEntity
        {
            Name = request.Name.Trim(),
            Description = CleanDescription(request.Description),
            IsArchived = request.IsArchived,
            Templates = [.. request.Templates.Select(t => new OnboardingTypeTemplateEntity { TemplateId = t.TemplateId, IsEnabled = t.IsEnabled })],
        };

        db.OnboardingTypes.Add(type);
        await db.SaveChangesAsync();

        return await GetSummary(type.Id);
    }

    public async Task<Result<OnboardingTypeDto>> Update(int id, SaveTypeRequest request)
    {
        OnboardingTypeEntity? type = await db.OnboardingTypes.Include(t => t.Templates).FirstOrDefaultAsync(t => t.Id == id);
        if (type is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        Result validation = await Validate(request, exceptId: id);
        if (validation.IsFailed)
        {
            return validation.ToResult<OnboardingTypeDto>();
        }

        string name = request.Name.Trim();
        string? description = CleanDescription(request.Description);
        var wanted = request.Templates.ToDictionary(t => t.TemplateId, t => t.IsEnabled);

        if (!ContentChanged(type, name, description, wanted))
        {
            await db.OnboardingTypes
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsArchived, request.IsArchived));
            return await GetSummary(id);
        }

        type.Name = name;
        type.Description = description;
        type.IsArchived = request.IsArchived;
        ApplyTemplates(type, wanted);

        db.Entry(type).State = EntityState.Modified;
        await db.SaveChangesAsync();

        return await GetSummary(id);
    }

    public async Task<Result<OnboardingTypeDto>> Duplicate(int id)
    {
        OnboardingTypeEntity? source = await db.OnboardingTypes
            .AsNoTracking()
            .Include(t => t.Templates)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (source is null)
        {
            return Result.Fail(new NotFoundError(ResourceName, id));
        }

        if (source.IsArchived)
        {
            return Result.Fail(new BusinessRuleError(ArchivedNotDuplicableMessage));
        }

        var copy = new OnboardingTypeEntity
        {
            Name = await NextCopyName(source.Name),
            Description = source.Description,
            IsArchived = false,
            Templates = [.. source.Templates.Select(t => new OnboardingTypeTemplateEntity { TemplateId = t.TemplateId, IsEnabled = t.IsEnabled })],
        };

        db.OnboardingTypes.Add(copy);
        await db.SaveChangesAsync();

        return await GetSummary(copy.Id);
    }

    public async Task<Result<OnboardingTypeDto>> SetArchived(int id, bool isArchived)
    {
        if (!isArchived)
        {
            bool? hasEnabledTemplate = await db.OnboardingTypes
                .Where(t => t.Id == id)
                .Select(t => (bool?)t.Templates.Any(x => x.IsEnabled && x.Template.IsActive))
                .FirstOrDefaultAsync();
            if (hasEnabledTemplate == false)
            {
                return Result.Fail(new BusinessRuleError(ActiveNeedsTemplateMessage, nameof(SaveTypeRequest.IsArchived)));
            }
        }

        // A direct update skips the audit stamp: archive and restore must not move "last edited".
        int updated = await db.OnboardingTypes
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsArchived, isArchived));

        return updated == 0 ? Result.Fail(new NotFoundError(ResourceName, id)) : await GetSummary(id);
    }

    private static bool ContentChanged(OnboardingTypeEntity type, string name, string? description, Dictionary<int, bool> wanted)
    {
        return type.Name != name
            || type.Description != description
            || type.Templates.Count != wanted.Count
            || type.Templates.Any(t => !wanted.TryGetValue(t.TemplateId, out bool enabled) || enabled != t.IsEnabled);
    }

    private static void ApplyTemplates(OnboardingTypeEntity type, Dictionary<int, bool> wanted)
    {
        foreach (OnboardingTypeTemplateEntity removed in type.Templates.Where(t => !wanted.ContainsKey(t.TemplateId)).ToList())
        {
            type.Templates.Remove(removed);
        }

        foreach (OnboardingTypeTemplateEntity existing in type.Templates)
        {
            existing.IsEnabled = wanted[existing.TemplateId];
        }

        foreach (KeyValuePair<int, bool> added in wanted.Where(w => type.Templates.All(t => t.TemplateId != w.Key)).ToList())
        {
            type.Templates.Add(new OnboardingTypeTemplateEntity { TemplateId = added.Key, IsEnabled = added.Value });
        }
    }

    private async Task<Result<OnboardingTypeDto>> GetSummary(int id)
    {
        OnboardingTypeDto? type = await ToDto(db.OnboardingTypes.Where(t => t.Id == id)).FirstOrDefaultAsync();
        return type is null ? Result.Fail(new NotFoundError(ResourceName, id)) : Result.Ok(type);
    }

    private IQueryable<OnboardingTypeDto> ToDto(IQueryable<OnboardingTypeEntity> types)
    {
        return types.AsNoTracking().Select(t => new OnboardingTypeDto(
            t.Id,
            t.Name,
            t.Description,
            t.IsArchived,
            t.Templates.Count(x => x.IsEnabled && x.Template.IsActive),
            db.OnboardingFlows.Count(f => f.OnboardingTypeId == t.Id),
            t.UpdatedAt));
    }

    private async Task<Result> Validate(SaveTypeRequest request, int? exceptId)
    {
        string name = request.Name.Trim();
        if (await db.OnboardingTypes.AnyAsync(t => t.Id != exceptId && t.Name.ToLower() == name.ToLower()))
        {
            return Result.Fail(new DuplicateResourceError(ResourceName, name, nameof(SaveTypeRequest.Name)));
        }

        List<int> templateIds = [.. request.Templates.Select(t => t.TemplateId)];
        if (templateIds.Distinct().Count() != templateIds.Count)
        {
            return Result.Fail(new BusinessRuleError("A template can be added to a type only once.", nameof(SaveTypeRequest.Templates)));
        }

        if (await db.OnboardingTaskTemplates.CountAsync(t => templateIds.Contains(t.Id)) != templateIds.Count)
        {
            return Result.Fail(new BusinessRuleError("One or more templates do not exist.", nameof(SaveTypeRequest.Templates)));
        }

        if (!await HasActiveEnabledTemplate(request.Templates.Where(t => t.IsEnabled).Select(t => t.TemplateId)))
        {
            return Result.Fail(new BusinessRuleError(TypeNeedsTemplateMessage, nameof(SaveTypeRequest.Templates)));
        }

        return Result.Ok();
    }

    private Task<bool> HasActiveEnabledTemplate(IEnumerable<int> enabledTemplateIds)
    {
        List<int> enabledIds = [.. enabledTemplateIds];
        return db.OnboardingTaskTemplates.AnyAsync(t => t.IsActive && enabledIds.Contains(t.Id));
    }

    private static string? CleanDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private async Task<string> NextCopyName(string name)
    {
        HashSet<string> taken = new(
            await db.OnboardingTypes.Select(t => t.Name).ToListAsync(), StringComparer.OrdinalIgnoreCase);

        // Copying a copy continues the original's numbering instead of stacking suffixes.
        string original = CopySuffix().Replace(name, string.Empty);

        for (int number = 1; ; number++)
        {
            string suffix = number == 1 ? " (copy)" : $" (copy {number})";
            string baseName = original[..Math.Min(original.Length, OnboardingTypeEntity.NameMaxLength - suffix.Length)];
            if (!taken.Contains(baseName + suffix))
            {
                return baseName + suffix;
            }
        }
    }

    [GeneratedRegex(@" \(copy( \d+)?\)$")]
    private static partial Regex CopySuffix();
}
