using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.Promotions.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.EmployeeActivities;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.FeatureFlags;
using LeoHR.Server.Shared.Models.Filter;
using LeoHR.Server.Shared.PromotionCriteria;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace LeoHR.Server.Features.Promotions;

public class PromotionsService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeLevelStart levelStart,
    IPromotionActivities promotionActivities,
    PromotionCriteriaTicksService criteriaTicks,
    IEmailService emailService,
    IFeatureManager features,
    IOptions<PromotionsOptions> promotionsOptions,
    IOptions<EmailSettings> emailSettings,
    ILogger<PromotionsService> logger) : IPromotionsService
{
    private readonly PromotionData data = new(db, employeeLookup);
    private readonly PromotionLevels promotionLevels = new(db);
    private readonly PromotionCreation creation = new(db, employeeLookup, promotionActivities, criteriaTicks, logger);

    public async Task<Result<PagedResult<PromotionDto>>> GetAll(ClaimsPrincipal user, PromotionFilterModel? filter = null)
    {
        IQueryable<PromotionEntity> query = db.Promotions;
        if (!string.IsNullOrWhiteSpace(filter?.EmployeeId))
        {
            Result access = await PromotionAccess.EnsureCanViewAsync(filter.EmployeeId, user, employeeLookup);
            if (access.IsFailed) return Result.Fail<PagedResult<PromotionDto>>(access.Errors);
            query = query.Where(p => p.EmployeeId == filter.EmployeeId);
        }
        else
        {
            query = query.ForUser(user);
        }

        query = query.WithStatus(filter?.Status);
        if (filter?.HasPendingActivities == true)
            query = query.WithPendingActivities(db.EmployeeActivities);

        // Both lookups return active employees only, so a leaver's promotions drop out of the list.
        List<string> employeeIds = string.IsNullOrWhiteSpace(filter?.SearchTerm)
            ? await data.GetActiveEmployeeIds()
            : await employeeLookup.SearchIdsByNameAsync(filter.SearchTerm.ToLower().Trim());
        employeeIds = await data.FilterByHrManagers(employeeIds, filter?.HrManagers);

        if (employeeIds.Count == 0) return new PagedResult<PromotionDto>([], 0);
        query = query.Where(p => employeeIds.Contains(p.EmployeeId));

        query = query.OrderByDescending(p => p.CreatedAt);
        int totalCount = await query.CountAsync();

        List<PromotionRow> rows = await query.Paginate(filter).ProjectToRow(db.EmployeeActivities).ToListAsync();
        List<EmployeeRef> employees = await employeeLookup.GetByIdsAsync([.. rows.Select(r => r.EmployeeId).Distinct()]);
        return new PagedResult<PromotionDto>(PromotionMapper.ToSummaryDtos(rows, employees), totalCount);
    }

    public async Task<Result<List<PendingApprovalDto>>> GetPendingApprovals(List<string>? hrManagers = null)
    {
        List<string> activeEmployeeIds = await data.FilterByHrManagers(await data.GetActiveEmployeeIds(), hrManagers);
        if (activeEmployeeIds.Count == 0) return Result.Ok(new List<PendingApprovalDto>());

        return Result.Ok(await data.LoadPendingApprovals(activeEmployeeIds));
    }

    public async Task<Result<PromotionDetailDto>> GetDetailByPromotionId(int id, ClaimsPrincipal user)
    {
        PromotionEntity? promotion = await db.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, id));

        Result access = await PromotionAccess.EnsureCanViewAsync(promotion.EmployeeId, user, employeeLookup);
        if (access.IsFailed) return Result.Fail<PromotionDetailDto>(access.Errors);

        return await GetPromotionDetails(promotion);
    }

    public async Task<Result<PromotionDetailDto>> GetDetailForCurrentUser(ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Result.Fail(new NotFoundError(PromotionMessages.Promotion, string.Empty));

        PromotionEntity? promotion = await data.LoadOpenPromotion(userId);
        if (promotion is null)
            return Result.Fail(new NotFoundError(PromotionMessages.Promotion, userId));

        return await GetPromotionDetails(promotion);
    }

    public async Task<Result<EmployeeLevelDto>> GetCurrentUserLevel(ClaimsPrincipal user)
    {
        string userId = user.GetUserId() ?? string.Empty;
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(userId);
        if (emp is null) return Result.Fail(new NotFoundError(PromotionMessages.Employee, userId));

        return new EmployeeLevelDto(emp.Seniority, emp.Profile);
    }

    public async Task<Result<PromotionDetailDto?>> GetActiveDetailForEmployee(string employeeId, ClaimsPrincipal user)
    {
        Result access = await PromotionAccess.EnsureCanViewAsync(employeeId, user, employeeLookup);
        if (access.IsFailed) return Result.Fail<PromotionDetailDto?>(access.Errors);

        PromotionEntity? promotion = await data.LoadOpenPromotion(employeeId);
        if (promotion is null)
            return Result.Ok<PromotionDetailDto?>(null);

        Result<PromotionDetailDto> details = await GetPromotionDetails(promotion);
        return details.IsFailed
            ? Result.Fail<PromotionDetailDto?>(details.Errors)
            : Result.Ok<PromotionDetailDto?>(details.Value);
    }

    public async Task<Result<PromotionDetailDto?>> GetByCompanyId(string companyId, ClaimsPrincipal user)
    {
        EmployeeRef? emp = await employeeLookup.GetByCompanyIdAsync(companyId);
        if (emp is null) return Result.Fail<PromotionDetailDto?>(new NotFoundError(PromotionMessages.Employee, companyId));

        return await GetActiveDetailForEmployee(emp.Id, user);
    }

    public async Task<Result<PromotionDto>> Create(CreatePromotionRequest request, ClaimsPrincipal user)
    {
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(request.EmployeeId);
        if (emp is null)
        {
            return Result.Fail(new NotFoundError(
                PromotionMessages.Employee, request.EmployeeId, nameof(CreatePromotionRequest.EmployeeId)));
        }

        if (await data.HasOpenPromotion(request.EmployeeId)) return Result.Fail(OpenPromotionExists(emp));

        Technology? technology = TechnologyParser.TryParse(emp.Profile);
        if (technology is null) return Result.Fail(new BusinessRuleError(PromotionMessages.NoPromotionTrack));
        if (technology != request.Technology)
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.NotEmployeesTrack, nameof(CreatePromotionRequest.Technology)));
        }

        string? fromRole = await promotionLevels.GetCurrentLevel(emp);
        if (fromRole is null) return Result.Fail(new BusinessRuleError(PromotionMessages.CurrentLevelUnknown));

        Result level = await promotionLevels.ValidateTargetLevel(
            emp.Id, request.Technology, fromRole, request.ToRole, nameof(CreatePromotionRequest.ToRole));
        if (level.IsFailed) return level;

        string holderId = string.IsNullOrWhiteSpace(request.PromotionHolderId)
            ? await data.GetDefaultHolderId(user, emp.Id)
            : request.PromotionHolderId;

        try
        {
            PromotionEntity promotion = await creation.Create(
                request with { FromRole = fromRole, PromotionHolderId = holderId }, emp);
            return PromotionMapper.ToSummaryDto(promotion, emp);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(LeoHrDbContext.OpenPromotionIndex))
        {
            return Result.Fail(OpenPromotionExists(emp));
        }
    }

    public async Task<Result<PromotionTargetLevelsDto>> GetTargetLevels(string employeeId)
    {
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(employeeId);
        if (emp is null) return Result.Fail(new NotFoundError(PromotionMessages.Employee, employeeId));

        Technology? technology = TechnologyParser.TryParse(emp.Profile);
        string? fromRole = await promotionLevels.GetCurrentLevel(emp);
        List<string> levels = technology is null || fromRole is null
            ? []
            : await promotionLevels.GetTargetLevels(emp.Id, technology.Value, fromRole);
        return new PromotionTargetLevelsDto(fromRole, technology, levels);
    }

    public async Task<int> CreateMissingPromotions()
    {
        List<EmployeeRef> employees = await employeeLookup.GetAllActiveAsync();
        HashSet<string> withOpenPromotion = await db.Promotions.Open().Select(p => p.EmployeeId).ToHashSetAsync();
        Dictionary<string, string> lastPromotedTo = await promotionLevels.GetLastPromotedToRoles();
        ILookup<Technology, PromotionLevel> levelsByTechnology =
            (await promotionLevels.GetActiveLevels()).ToLookup(level => level.Technology);

        // No signed-in HR here, so the company default stands in for a per-user setting.
        string defaultHolderId = employees.Any(e => e.Id == AppSettingsEntity.DefaultPromotionHolder)
            ? AppSettingsEntity.DefaultPromotionHolder
            : string.Empty;

        int created = 0;
        foreach (EmployeeRef emp in employees.Where(e => !withOpenPromotion.Contains(e.Id)))
        {
            CreatePromotionRequest? request = PromotionLevels.BuildMissingPromotionRequest(
                emp, lastPromotedTo.GetValueOrDefault(emp.Id), levelsByTechnology, defaultHolderId);
            // Re-checked per employee: HR can create one by hand while this loop runs.
            if (request is null || await data.HasOpenPromotion(emp.Id)) continue;

            try
            {
                await creation.Create(request, emp);
                created++;
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation(LeoHrDbContext.OpenPromotionIndex))
            {
                db.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Clear();
                logger.LogError(ex, "Creating the missing promotion for {EmployeeId} failed", emp.Id);
            }
        }

        return created;
    }

    public async Task<int> DeleteLeaversPromotions()
    {
        Dictionary<string, bool> activeById = await employeeLookup.GetActiveFlagsByIdAsync();
        List<string> openEmployeeIds = await db.Promotions.Open().Select(p => p.EmployeeId).Distinct().ToListAsync();

        List<string> missingIds = [.. openEmployeeIds.Where(id => !activeById.ContainsKey(id))];
        if (missingIds.Count > 0)
        {
            logger.LogWarning(
                "Kept the open promotions of {Count} employees missing from the Employee API: {EmployeeIds}",
                missingIds.Count, missingIds);
        }

        List<string> leaverIds = [.. openEmployeeIds.Where(id => activeById.TryGetValue(id, out bool active) && !active)];
        if (leaverIds.Count == 0) return 0;

        List<PromotionEntity> leaversPromotions = await db.Promotions
            .Open()
            .Where(p => leaverIds.Contains(p.EmployeeId))
            .ToListAsync();
        int maxRemovals = promotionsOptions.Value.MaxLeaversPerCleanup;
        if (leaversPromotions.Count > maxRemovals)
        {
            logger.LogError(
                "Removed no leavers' promotions: {Count} would be removed, over the limit of {Max}",
                leaversPromotions.Count, maxRemovals);
            return 0;
        }

        foreach (PromotionEntity promotion in leaversPromotions)
        {
            promotion.IsDeleted = true;
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await promotionActivities.DeleteAllForPromotionsAsync([.. leaversPromotions.Select(p => p.Id)]);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return leaversPromotions.Count;
    }

    public async Task<int> NotifyReadyPromotions()
    {
        List<PromotionEntity> ready = await db.Promotions
            .Where(p => p.Status == PromotionStatus.Ready && p.ReadyEmailSentAt == null)
            .ToListAsync();
        if (ready.Count == 0) return 0;

        // Marked before sending: a failed email is logged, never retried, so HR is never told twice.
        foreach (PromotionEntity promotion in ready) promotion.ReadyEmailSentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        int sent = 0;
        foreach (PromotionEntity promotion in ready)
        {
            Result result = await SendReadyEmail(promotion);
            if (result.IsSuccess)
            {
                sent++;
            }
            else
            {
                logger.LogError("The Ready to Send email for promotion {PromotionId} could not be sent: {Reasons}",
                    promotion.Id, string.Join("; ", result.Errors.Select(e => e.Message)));
            }
        }

        return sent;
    }

    private async Task<Result> SendReadyEmail(PromotionEntity promotion)
    {
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(promotion.EmployeeId);
        if (emp is null) return Result.Fail(new NotFoundError(PromotionMessages.Employee, promotion.EmployeeId));

        var parameters = new Dictionary<string, string>
        {
            ["firstName"] = emp.FirstName,
            ["lastName"] = emp.LastName,
            ["toRole"] = promotion.ToRole,
            ["promotionLink"] = emailSettings.Value.PromotionUrl(promotion.Id),
        };
        return await emailService.SendForEmployeeAsync(EmailTemplateCatalog.PromotionReady, emp.Id, parameters);
    }

    public async Task<Result> Update(int promotionId, UpdatePromotionRequest request)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));
        if (promotion.Status == PromotionStatus.Sent)
            return Result.Fail(new BusinessRuleError(PromotionMessages.SentPromotionNotEditable));

        if (request.PromotionHolderId != promotion.PromotionHolderId)
        {
            Result holder = await ValidateHolder(promotion, request.PromotionHolderId);
            if (holder.IsFailed) return holder;
            promotion.PromotionHolderId = request.PromotionHolderId;
        }

        if (request.ToRole != promotion.ToRole)
        {
            Result level = await promotionLevels.ValidateTargetLevel(
                promotion.EmployeeId, promotion.Technology, promotion.FromRole, request.ToRole,
                nameof(UpdatePromotionRequest.ToRole));
            if (level.IsFailed) return level;

            promotion.ToRole = request.ToRole;
            await ReevaluateCriteriaForNewLevel(promotion);
            await SyncStatusWithCriteria(promotion);
        }

        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<EmailRecipients>> GetEmailRecipients(int promotionId)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));

        return await emailService.ResolveRecipientsAsync(
            EmailTemplateCatalog.Promotion, promotion.EmployeeId, await data.GetHolderRole(promotion));
    }

    public async Task<Result> SendPromotionEmail(int promotionId, SendPromotionRequest request, ClaimsPrincipal user)
    {
        // Locked until the send is recorded: a second click waits, then finds the promotion already Sent.
        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlAsync($"SELECT 1 FROM promotions WHERE id = {promotionId} FOR UPDATE");

        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));
        if (promotion.Status != PromotionStatus.Ready)
            return Result.Fail(new BusinessRuleError(PromotionMessages.PromotionNotReadyToSend));

        // The stored status isn't enough: an older row can be Ready with an open criterion or short points.
        // Once the email went out, a retry only finishes the send.
        if (promotion.EmailSentAt is null
            && (!await criteriaTicks.SyncScore(promotion) || promotion.Status != PromotionStatus.Ready))
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Fail(new BusinessRuleError(PromotionMessages.CriteriaNotMet));
        }

        if (await GetKnowledgeEvaluationStatus(promotionId) == KnowledgeEvaluationStatus.Planned)
            return Result.Fail(new BusinessRuleError(PromotionMessages.KePlannedBlocksSend));

        EmployeeRef? emp = await employeeLookup.GetByIdAsync(promotion.EmployeeId);
        if (emp is null) return Result.Fail(new NotFoundError(PromotionMessages.Employee, promotion.EmployeeId));

        if (promotion.EmailSentAt is null)
        {
            Result sent = await SendEmail(promotion, emp, request);
            if (sent.IsFailed) return sent;

            promotion.EmailSentAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        // If recording the send fails, EmailSentAt is still committed.
        await transaction.CreateSavepointAsync(nameof(MarkSent));
        try
        {
            await MarkSent(promotion, emp);
        }
        catch
        {
            await transaction.RollbackToSavepointAsync(nameof(MarkSent));
            await transaction.CommitAsync();
            throw;
        }
        await transaction.CommitAsync();

        await creation.CreateNext(promotion, emp, user);
        return Result.Ok();
    }

    private async Task<KnowledgeEvaluationStatus?> GetKnowledgeEvaluationStatus(int promotionId)
    {
        if (!await features.IsEnabledAsync(FeatureFlagNames.KnowledgeEvaluation)) return null;

        List<KnowledgeEvaluationStatus> statuses = await db.KnowledgeEvaluations
            .Where(e => e.PromotionId == promotionId)
            .OrderByDescending(e => e.Id)
            .Select(e => e.Status)
            .ToListAsync();
        return statuses.Contains(KnowledgeEvaluationStatus.Planned)
            ? KnowledgeEvaluationStatus.Planned
            : statuses.Cast<KnowledgeEvaluationStatus?>().FirstOrDefault();
    }

    public async Task<Result> SetCriterion(int promotionId, SetCriterionRequest request)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));
        if (promotion.Status == PromotionStatus.Sent)
            return Result.Fail(new BusinessRuleError(PromotionMessages.SentPromotionReadOnly));

        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        if (criteria is null || !PromotionRules.RequiredCriteriaLabels(criteria).Contains(request.Label))
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.UnknownCriterion, nameof(SetCriterionRequest.Label)));
        }

        if (request.Label == CriteriaLabels.ActivityScore)
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.ActivityScoreAutomatic, nameof(SetCriterionRequest.Label)));
        }

        // The tick comes from a Successful knowledge evaluation, which can't be edited afterwards.
        if (!request.Met && request.Label == CriteriaLabels.KeRequired
            && await db.KnowledgeEvaluations.AnyAsync(e => e.PromotionId == promotionId
                && e.Status == KnowledgeEvaluationStatus.Successful))
        {
            return Result.Fail(new BusinessRuleError(PromotionMessages.KeSuccessfulLocked));
        }

        // Marking again would drop the comment of whoever marked it first, e.g. from a stale page.
        if (request.Met && promotion.CriteriaOverrides.Any(o => o.Label == request.Label)) return Result.Ok();

        List<string> labels = [.. promotion.ApprovedCriteriaLabels.Where(l => l != request.Label)];
        List<CriterionOverride> marks = [.. promotion.CriteriaOverrides.Where(o => o.Label != request.Label)];
        if (request.Met)
        {
            labels.Add(request.Label);
            marks.Add(new CriterionOverride(request.Label, request.Comment?.Trim() ?? string.Empty));
        }
        promotion.ApprovedCriteriaLabels = labels;
        promotion.CriteriaOverrides = marks;

        await criteriaTicks.SyncScore(promotion);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> VerifyCriteria(int promotionId, ClaimsPrincipal user)
    {
        PromotionEntity? promotion = await db.Promotions.FirstOrDefaultAsync(p => p.Id == promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));

        Result access = PromotionAccess.EnsureCanModify(promotion, user);
        if (access.IsFailed) return access;
        if (!PromotionRules.NeedsCriteriaVerification(promotion))
        {
            await criteriaTicks.SyncScore(promotion);
            await db.SaveChangesAsync();
            return Result.Ok();
        }

        // Without criteria for the level there is nothing to verify yet; the stamp waits for them.
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(promotion.EmployeeId);
        if (!await criteriaTicks.SyncAutoCriteria(promotion, emp)) return Result.Ok();

        promotion.CriteriaVerifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<PromotionActivityDto>> AddActivity(
        int promotionId,
        AddActivityRequest request,
        ClaimsPrincipal user)
    {
        Result<PromotionEntity> load = await LoadModifiablePromotion(promotionId, user);
        if (load.IsFailed) return Result.Fail<PromotionActivityDto>(load.Errors);
        PromotionEntity promotion = load.Value;

        ActivityEntity? activity = await db.Activities.FindAsync(request.ActivityId);
        if (activity is null) return Result.Fail(new NotFoundError(PromotionMessages.Activity, request.ActivityId));

        if (await data.IsActivityAlreadyAdded(promotion.Id, request.ActivityId))
            return Result.Fail(new DuplicateResourceError(PromotionMessages.ActivityAlreadyAdded));

        List<EmployeeActivityEntity> added =
            await promotionActivities.AddPlannedAsync(promotion, [activity], request.Note);
        await db.SaveChangesAsync();
        return PromotionMapper.MapActivity(added.Single());
    }

    public async Task<Result<AddActivitiesResult>> AddActivitiesToPromotion(
        int[] activityIds,
        int promotionId,
        ClaimsPrincipal user)
    {
        Result<PromotionEntity> load = await LoadModifiablePromotion(promotionId, user);
        if (load.IsFailed) return Result.Fail<AddActivitiesResult>(load.Errors);
        PromotionEntity promotion = load.Value;

        List<ActivityEntity> activities = await db.Activities.Where(a => activityIds.Contains(a.Id) && a.IsActive).ToListAsync();
        var foundIds = activities.Select(a => a.Id).ToHashSet();
        var notFound = activityIds.Where(id => !foundIds.Contains(id)).ToList();

        HashSet<int> existingIds = await db.EmployeeActivities
            .Where(ea => ea.PromotionId == promotion.Id && activityIds.Contains(ea.ActivityId))
            .Select(ea => ea.ActivityId)
            .ToHashSetAsync();

        var newActivities = activities.Where(a => !existingIds.Contains(a.Id)).ToList();
        if (newActivities.Count == 0)
            return new AddActivitiesResult([], notFound, [.. existingIds]);

        List<EmployeeActivityEntity> added =
            await promotionActivities.AddPlannedAsync(promotion, newActivities, note: string.Empty);
        await db.SaveChangesAsync();
        return new AddActivitiesResult([.. added.Select(PromotionMapper.MapActivity)], notFound,
            [.. existingIds]);
    }

    public async Task<Result> RemoveActivity(int promotionId, int employeeActivityId, ClaimsPrincipal user)
    {
        Result<PromotionEntity> load = await LoadModifiablePromotion(promotionId, user);
        if (load.IsFailed) return Result.Fail(load.Errors);

        return await promotionActivities.RemovePlannedAsync(promotionId, employeeActivityId, user);
    }

    public async Task<Result> ApproveActivity(
        int promotionId,
        int employeeActivityId,
        DateOnly accomplishedOn,
        string? approvedById,
        ClaimsPrincipal user)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));
        if (promotion.Status == PromotionStatus.Sent)
            return Result.Fail(new BusinessRuleError(PromotionMessages.SentPromotionReadOnly));

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        Result approve = await promotionActivities.ApproveAsync(
            promotion.EmployeeId, employeeActivityId, accomplishedOn, approvedById, user.GetUserFullName());
        if (approve.IsFailed) return approve;

        await criteriaTicks.SyncScore(promotion);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Ok();
    }

    public async Task<Result> RevertActivityApproval(int promotionId, int employeeActivityId)
    {
        PromotionEntity? promotion = await db.Promotions.FindAsync(promotionId);
        if (promotion is null) return Result.Fail(new NotFoundError(PromotionMessages.Promotion, promotionId));
        if (promotion.Status == PromotionStatus.Sent)
            return Result.Fail(new BusinessRuleError(PromotionMessages.SentPromotionReadOnly));

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        Result revert = await promotionActivities.RevertApprovalAsync(promotionId, employeeActivityId);
        if (revert.IsFailed) return revert;

        await criteriaTicks.SyncScore(promotion);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Ok();
    }

    private async Task<PromotionDetailDto> GetPromotionDetails(PromotionEntity promotion)
    {
        EmployeeRef? emp = await employeeLookup.GetByIdAsync(promotion.EmployeeId);
        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        DateTime? levelStartDate = promotion.LevelStartDate;
        DateTime refDate = promotion.ReferenceDate ?? DateTime.UtcNow;

        List<EmployeeActivityEntity> activities = await data.LoadPromotionActivities(promotion);
        ActivityBuckets buckets = PromotionMapper.BuildActivityBuckets(activities, refDate, levelStartDate);
        List<PromotionHistoryItem> history = await data.LoadHistory(promotion);
        EmployeeRef? holder = await employeeLookup.GetByIdAsync(promotion.PromotionHolderId);
        List<string> targetLevels =
            promotion.Status == PromotionStatus.Sent ? [] : await promotionLevels.GetTargetLevels(promotion);

        KnowledgeEvaluationStatus? keStatus = await GetKnowledgeEvaluationStatus(promotion.Id);

        return PromotionMapper.BuildDetailDto(promotion, emp, criteria, buckets, history, holder, targetLevels, keStatus);
    }

    // Runs in the send's transaction, so Sent and the new level start land together: the next promotion snapshots it.
    private async Task MarkSent(PromotionEntity promotion, EmployeeRef emp)
    {
        DateTime now = DateTime.UtcNow;
        promotion.Status = PromotionStatus.Sent;
        promotion.ReferenceDate = now;
        promotion.HrManager = string.IsNullOrWhiteSpace(emp.HrManager) ? null : emp.HrManager.Trim();
        await db.SaveChangesAsync();
        await levelStart.SetLevelStartDateAsync(promotion.EmployeeId, now);
    }

    private static DuplicateResourceError OpenPromotionExists(EmployeeRef emp)
    {
        return new DuplicateResourceError($"{PromotionMessages.Promotion} for {PromotionMapper.FullName(emp)} already exists.");
    }

    private async Task<Result> SendEmail(PromotionEntity promotion, EmployeeRef emp, SendPromotionRequest request)
    {
        var parameters = new Dictionary<string, string>
        {
            ["firstName"] = emp.FirstName,
            ["lastName"] = emp.LastName,
            ["fromRole"] = promotion.FromRole,
            ["toRole"] = promotion.ToRole,
        };

        Result<List<Recipient>> extraCc = await data.FindExtraCc(request.ExtraCcEmployeeIds);
        if (extraCc.IsFailed) return Result.Fail(extraCc.Errors);

        return await emailService.SendForEmployeeAsync(
            EmailTemplateCatalog.Promotion, emp.Id, parameters, await data.GetHolderRole(promotion), extraCc.Value);
    }

    private async Task<Result> ValidateHolder(PromotionEntity promotion, string holderId)
    {
        if (holderId == promotion.EmployeeId)
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.HolderIsPromotedEmployee, nameof(UpdatePromotionRequest.PromotionHolderId)));
        }

        return await employeeLookup.GetByIdAsync(holderId) is null
            ? Result.Fail(new NotFoundError(
                PromotionMessages.Employee, holderId, nameof(UpdatePromotionRequest.PromotionHolderId)))
            : Result.Ok();
    }

    // The old level's ticks and criteria may not hold for the new one, so the labels and the criteria link start over.
    private async Task ReevaluateCriteriaForNewLevel(PromotionEntity promotion)
    {
        promotion.ApprovedCriteriaLabels = [];
        promotion.CriteriaOverrides = [];
        promotion.PromotionCriteriaId = null;
        await criteriaTicks.SyncAutoCriteria(promotion, await employeeLookup.GetByIdAsync(promotion.EmployeeId));
    }

    private async Task SyncStatusWithCriteria(PromotionEntity promotion)
    {
        PromotionCriteriaDto? criteria = await db.PromotionCriteria.ForPromotionAsync(promotion);
        if (criteria is not null) PromotionRules.SyncStatus(promotion, criteria);
    }

    private async Task<Result<PromotionEntity>> LoadModifiablePromotion(int promotionId, ClaimsPrincipal user)
    {
        PromotionEntity? promotion = await db.Promotions.FirstOrDefaultAsync(p => p.Id == promotionId);
        if (promotion is null)
            return Result.Fail<PromotionEntity>(new NotFoundError(PromotionMessages.Promotion, promotionId));

        Result access = PromotionAccess.EnsureCanModify(promotion, user);
        if (access.IsFailed) return Result.Fail<PromotionEntity>(access.Errors);

        if (promotion.Status != PromotionStatus.InProgress)
            return Result.Fail<PromotionEntity>(new BusinessRuleError(PromotionMessages.PromotionNotInProgress));

        return promotion;
    }
}
