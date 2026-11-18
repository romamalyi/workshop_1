using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Features.EmployeeActivities.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Promotions;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeoHR.Server.Features.EmployeeActivities;

public interface IEmployeeActivitiesService
{
    Task<List<EmployeeActivityDto>> GetAll(string employeeId, EmployeeActivityStatus? status);
    Task<EmployeeActivityDto?> GetById(string employeeId, int id);
    Task<Result<EmployeeActivityDto>> Create(string employeeId, CreateEmployeeActivityRequest request, ClaimsPrincipal user);
    Task<Result<EmployeeActivityDto>> Update(string employeeId, int id, UpdateEmployeeActivityRequest request, ClaimsPrincipal user);
    Task<Result<EmployeeActivityDto>> SelfReport(string employeeId, int id, SelfReportRequest request);
    Task<Result<EmployeeActivityDto>> Approve(
        string employeeId, int id, DateOnly accomplishedOn, string? approvedById, ClaimsPrincipal user);
    Task<Result> Delete(string employeeId, int id, ClaimsPrincipal user);
    Task<List<PlannedActivityDto>> GetPlanned(PlannedActivityFilter filter);
    Task<Result> UpdatePlannedDate(string employeeId, int id, DateOnly plannedAt);
    Task<List<IntegrationActivityDto>> GetIntegrationActivities();
    Task<Result> RecordEmployeeActivity(RecordEmployeeActivityRequest request, ClaimsPrincipal user);
    Task<Result<List<IntegrationEmployeeActivityDto>>> GetIntegrationEmployeeActivities(
        IntegrationEmployeeActivitiesFilter filter, ClaimsPrincipal user);
    Task<Result> UpdateIntegrationEmployeeActivity(int id, UpdateIntegrationEmployeeActivityRequest request, ClaimsPrincipal user);
}

public class EmployeeActivitiesService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IPromotionCriteriaTicks criteriaTicks,
    ILogger<EmployeeActivitiesService> logger)
    : IEmployeeActivitiesService
{
    private const int MaxPlannedActivities = 1000;

    private readonly EmployeeActivityData data = new(db);

    public async Task<List<EmployeeActivityDto>> GetAll(string employeeId, EmployeeActivityStatus? status)
    {
        IQueryable<EmployeeActivityEntity> query = db.EmployeeActivities
            .AsNoTracking()
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            // CarriedFromPromotionId == null: exclude carry-over clones, which would double-list an activity.
            .Where(x => x.EmployeeId == employeeId && x.CarriedFromPromotionId == null);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        List<EmployeeActivityDto> activities = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => EmployeeActivityMapper.MapToDto(x))
            .ToListAsync();
        return await WithCreatorNames(activities);
    }

    public async Task<EmployeeActivityDto?> GetById(string employeeId, int id)
    {
        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .AsNoTracking()
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();
        return entity is null ? null : (await WithCreatorNames([EmployeeActivityMapper.MapToDto(entity)]))[0];
    }

    public async Task<Result<EmployeeActivityDto>> Create(string employeeId, CreateEmployeeActivityRequest request, ClaimsPrincipal user)
    {
        if (!user.CanWriteManage() && request.Status == EmployeeActivityStatus.Approved)
        {
            return Result.Fail(new ForbiddenError(EmployeeActivityMessages.OnlyAdminsCreateApproved));
        }

        if ((request.Status == EmployeeActivityStatus.SelfReported ||
             request.Status == EmployeeActivityStatus.Approved)
            && request.AccomplishedOn is null)
        {
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.AccomplishedOnRequired));
        }

        Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(request.Status, request.AccomplishedOn);
        if (dateCheck.IsFailed) return Result.Fail<EmployeeActivityDto>(dateCheck.Errors);

        ActivityEntity? activity = await db.Activities.FindAsync(request.ActivityId);
        if (activity is null)
        {
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.Activity, request.ActivityId));
        }

        Result duplicateCheck = await data.ValidateDuplicateActivityAsync(employeeId, activity);
        if (duplicateCheck.IsFailed) return Result.Fail<EmployeeActivityDto>(duplicateCheck.Errors);

        if (request.PromotionId is { } requestedPromotionId)
        {
            if (!await data.IsEmployeePromotion(employeeId, requestedPromotionId))
                return Result.Fail(new NotFoundError(EmployeeActivityMessages.Promotion, requestedPromotionId));
            if (await data.IsSentPromotion(requestedPromotionId))
                return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));
        }

        int? promotionId = request.PromotionId ?? await data.ResolveActivePromotionId(employeeId);

        var entity = new EmployeeActivityEntity
        {
            EmployeeId = employeeId,
            ActivityId = request.ActivityId,
            Activity = activity,
            Status = request.Status,
            AccomplishedOn = request.AccomplishedOn,
            Note = request.Note,
            PromotionId = promotionId,
        };

        if (request.Status == EmployeeActivityStatus.Approved)
            EmployeeActivityRules.MarkApproved(entity, activity, request.AccomplishedOn, request.CreatedById, user.GetUserFullName());

        db.EmployeeActivities.Add(entity);
        await SaveWithCriteriaTicks(entity);

        if (entity.PromotionId.HasValue)
            await db.Entry(entity).Reference(x => x.Promotion).LoadAsync();

        return Result.Ok(EmployeeActivityMapper.MapToDto(entity));
    }

    public async Task<Result<EmployeeActivityDto>> Update(string employeeId, int id, UpdateEmployeeActivityRequest request, ClaimsPrincipal user)
    {
        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();

        if (entity is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.EmployeeActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));

        if (entity.Status == EmployeeActivityStatus.Approved && !user.CanWriteManage())
            return Result.Fail(new ForbiddenError(EmployeeActivityMessages.ApprovedNotEditable));

        bool activityChanged = request.ActivityId.HasValue && request.ActivityId.Value != entity.ActivityId;

        if (activityChanged)
        {
            ActivityEntity? activity = await db.Activities.FindAsync(request.ActivityId.Value);
            if (activity is null) return Result.Fail(new NotFoundError(EmployeeActivityMessages.Activity, request.ActivityId.Value));
            entity.ActivityId = activity.Id;
            entity.Activity = activity;
            // The snapshot is taken at approval, so only an approved record carries one.
            if (entity.Status == EmployeeActivityStatus.Approved) entity.PointsSnapshot = activity.Points;
        }

        if (request.AccomplishedOn.HasValue)
        {
            // entity.Status, not a request field — UpdateEmployeeActivityRequest can't change status.
            Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(entity.Status, request.AccomplishedOn);
            if (dateCheck.IsFailed) return Result.Fail<EmployeeActivityDto>(dateCheck.Errors);
        }

        entity.AccomplishedOn = request.AccomplishedOn ?? entity.AccomplishedOn;
        entity.Note = request.Note ?? entity.Note;

        await SaveWithCriteriaTicks(entity);
        return Result.Ok(EmployeeActivityMapper.MapToDto(entity));
    }

    public async Task<Result<EmployeeActivityDto>> SelfReport(string employeeId, int id, SelfReportRequest request)
    {
        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();

        if (entity is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.EmployeeActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));

        if (entity.Status != EmployeeActivityStatus.Planned)
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.AlreadySelfReportedOrApproved));

        Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(EmployeeActivityStatus.SelfReported, request.AccomplishedOn);
        if (dateCheck.IsFailed) return Result.Fail<EmployeeActivityDto>(dateCheck.Errors);

        entity.Status = EmployeeActivityStatus.SelfReported;
        entity.AccomplishedOn = request.AccomplishedOn;
        entity.Note = request.Note ?? entity.Note;

        await db.SaveChangesAsync();
        return Result.Ok(EmployeeActivityMapper.MapToDto(entity));
    }

    public async Task<Result<EmployeeActivityDto>> Approve(
        string employeeId, int id, DateOnly accomplishedOn, string? approvedById, ClaimsPrincipal user)
    {
        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();

        if (entity is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.EmployeeActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));

        if (entity.Status == EmployeeActivityStatus.Approved)
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.AlreadyApproved));
        if (entity.Status == EmployeeActivityStatus.Planned)
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.PlannedNotApprovable));

        Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(EmployeeActivityStatus.Approved, accomplishedOn);
        if (dateCheck.IsFailed) return Result.Fail<EmployeeActivityDto>(dateCheck.Errors);

        EmployeeActivityRules.MarkApproved(entity, entity.Activity, accomplishedOn, approvedById, user.GetUserFullName());

        await SaveWithCriteriaTicks(entity);
        return Result.Ok(EmployeeActivityMapper.MapToDto(entity));
    }

    public async Task<Result> Delete(string employeeId, int id, ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();

        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();

        if (entity is null) return Result.Fail(new NotFoundError(EmployeeActivityMessages.EmployeeActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity)) return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));
        if (!user.CanWriteManage() && entity.CreatedById != userId)
            return Result.Fail(new ForbiddenError(EmployeeActivityMessages.CannotDeleteOthersActivity));

        db.EmployeeActivities.Remove(entity);
        await SaveWithCriteriaTicks(entity);
        return Result.Ok();
    }

    public async Task<List<PlannedActivityDto>> GetPlanned(PlannedActivityFilter filter)
    {
        // Active employees only, so a leaver's planned activities drop out of the list.
        var byId = (await employeeLookup.GetAllActiveAsync()).WithHrManagers(filter.HrManagers).ToDictionary(e => e.Id);
        if (byId.Count == 0) return [];

        List<string> employeeIds = [.. byId.Keys];
        IQueryable<EmployeeActivityEntity> query = db.EmployeeActivities
            .AsNoTracking()
            .Where(a => a.Status == EmployeeActivityStatus.Planned
                     && employeeIds.Contains(a.EmployeeId)
                     && (a.Promotion == null || a.Promotion.Status != PromotionStatus.Sent));
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim().ToLower();
            List<string> matchingEmployeeIds = [.. await employeeLookup.SearchIdsByNameAsync(filter.SearchTerm)];
            query = query.Where(a => matchingEmployeeIds.Contains(a.EmployeeId) || a.Activity.Name.ToLower().Contains(term));
        }
        if (filter.Categories is { Count: > 0 } categories)
            query = query.Where(a => categories.Contains(a.Activity.Category));

        var rows = await query
            .OrderBy(a => a.PlannedAt ?? a.CreatedAt)
            .ThenBy(a => a.Id)
            .Take(MaxPlannedActivities)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.PromotionId,
                a.Activity.Category,
                a.Activity.Name,
                PlannedAt = a.PlannedAt ?? a.CreatedAt,
            })
            .ToListAsync();

        return [.. rows.Select(r =>
        {
            EmployeeRef emp = byId[r.EmployeeId];
            return new PlannedActivityDto(
                EmployeeActivityId: r.Id,
                EmployeeId: r.EmployeeId,
                PromotionId: r.PromotionId,
                EmployeeName: $"{emp.FirstName} {emp.LastName}".Trim(),
                Category: r.Category.ToString(),
                Name: r.Name,
                PlannedAt: r.PlannedAt,
                HrManager: emp.HrManager);
        })];
    }

    public async Task<Result> UpdatePlannedDate(string employeeId, int id, DateOnly plannedAt)
    {
        if (plannedAt > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.FuturePlannedDate));

        EmployeeActivityEntity? entity = await data.FindUserActivity(employeeId, id)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync();
        if (entity is not { Status: EmployeeActivityStatus.Planned })
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.PlannedActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));

        entity.PlannedAt = plannedAt.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public Task<List<IntegrationActivityDto>> GetIntegrationActivities()
    {
        return data.IntegrationActivities()
            .AsNoTracking()
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Name)
            .Select(a => new IntegrationActivityDto(a.Id, a.Category, a.Name, a.Points, a.WithinPeriod))
            .ToListAsync();
    }

    public async Task<Result> RecordEmployeeActivity(RecordEmployeeActivityRequest request, ClaimsPrincipal user)
    {
        DateOnly accomplishedOn = request.AccomplishedOn!.Value;
        Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(EmployeeActivityStatus.Approved, accomplishedOn);
        if (dateCheck.IsFailed) return dateCheck;

        ActivityEntity? activity = await data.IntegrationActivities().FirstOrDefaultAsync(a => a.Id == request.ActivityId);
        if (activity is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.Activity, request.ActivityId));

        if (await employeeLookup.GetByIdAsync(request.EmployeeId) is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.Employee, request.EmployeeId));

        List<EmployeeActivityEntity> existing = await db.EmployeeActivities
            .Include(x => x.Promotion)
            .Where(x => x.EmployeeId == request.EmployeeId
                && x.ActivityId == activity.Id
                && x.CarriedFromPromotionId == null)
            .ToListAsync();

        if (existing.Any(x => x.Status == EmployeeActivityStatus.Approved && x.AccomplishedOn == accomplishedOn))
            return Result.Ok();

        EmployeeActivityEntity? entity = existing.FirstOrDefault(x =>
            x.Status != EmployeeActivityStatus.Approved && !EmployeeActivityRules.IsInSentPromotion(x));
        if (entity is null)
        {
            Result duplicateCheck = await data.ValidateDuplicateActivityAsync(request.EmployeeId, activity);
            if (duplicateCheck.IsFailed) return duplicateCheck;

            entity = new EmployeeActivityEntity
            {
                EmployeeId = request.EmployeeId,
                ActivityId = activity.Id,
                Activity = activity,
                PromotionId = await data.ResolveActivePromotionId(request.EmployeeId),
            };
            db.EmployeeActivities.Add(entity);
        }

        // Without a client_id the record keeps the old shared marker, so the POST contract is unchanged.
        string approvedByName = EmployeeActivityRules.IntegrationName(user) ?? EmployeeActivityMessages.ExternalIntegration;
        EmployeeActivityRules.MarkApproved(entity, activity, accomplishedOn, approvedById: null, approvedByName);
        entity.Note = request.Note ?? entity.Note;

        await SaveWithCriteriaTicks(entity);
        return Result.Ok();
    }

    public async Task<Result<List<IntegrationEmployeeActivityDto>>> GetIntegrationEmployeeActivities(
        IntegrationEmployeeActivitiesFilter filter, ClaimsPrincipal user)
    {
        string? integrationName = EmployeeActivityRules.IntegrationName(user);
        if (integrationName is null)
            return Result.Fail(new ForbiddenError(EmployeeActivityMessages.IntegrationNotIdentified));

        List<IntegrationEmployeeActivityDto> activities = await data.IntegrationOwned(integrationName)
            .AsNoTracking()
            .Where(x => x.EmployeeId == filter.EmployeeId
                && (filter.ActivityId == null || x.ActivityId == filter.ActivityId)
                && (filter.AccomplishedOn == null || x.AccomplishedOn == filter.AccomplishedOn))
            .OrderBy(x => x.AccomplishedOn)
            .ThenBy(x => x.Id)
            .Select(x => new IntegrationEmployeeActivityDto(
                x.Id,
                x.ActivityId,
                x.Activity.Name,
                x.AccomplishedOn,
                x.PointsSnapshot,
                x.Note,
                x.Promotion == null || x.Promotion.Status != PromotionStatus.Sent))
            .ToListAsync();
        return Result.Ok(activities);
    }

    public async Task<Result> UpdateIntegrationEmployeeActivity(
        int id, UpdateIntegrationEmployeeActivityRequest request, ClaimsPrincipal user)
    {
        string? integrationName = EmployeeActivityRules.IntegrationName(user);
        if (integrationName is null)
            return Result.Fail(new ForbiddenError(EmployeeActivityMessages.IntegrationNotIdentified));

        EmployeeActivityEntity? entity = await data.IntegrationOwned(integrationName)
            .Include(x => x.Activity)
            .Include(x => x.Promotion)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null)
            return Result.Fail(new NotFoundError(EmployeeActivityMessages.EmployeeActivity, id));
        if (EmployeeActivityRules.IsInSentPromotion(entity))
            return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.SentPromotionReadOnly));

        DateOnly accomplishedOn = request.AccomplishedOn!.Value;
        Result dateCheck = EmployeeActivityRules.ValidateAccomplishedOn(EmployeeActivityStatus.Approved, accomplishedOn);
        if (dateCheck.IsFailed) return dateCheck;

        if (request.ActivityId != entity.ActivityId)
        {
            ActivityEntity? activity = await data.IntegrationActivities().FirstOrDefaultAsync(a => a.Id == request.ActivityId);
            if (activity is null)
                return Result.Fail(new NotFoundError(EmployeeActivityMessages.Activity, request.ActivityId));

            // Skipped for an unchanged activity, which would count the record against itself.
            Result duplicateCheck = await data.ValidateDuplicateActivityAsync(entity.EmployeeId, activity);
            if (duplicateCheck.IsFailed) return duplicateCheck;

            entity.ActivityId = activity.Id;
            entity.Activity = activity;
            entity.PointsSnapshot = activity.Points;
        }

        entity.AccomplishedOn = accomplishedOn;
        entity.Note = request.Note ?? entity.Note;

        await SaveWithCriteriaTicks(entity);
        return Result.Ok();
    }

    // Promotions re-checks the score in the same transaction as the save.
    private async Task SaveWithCriteriaTicks(EmployeeActivityEntity entity)
    {
        if (entity.Status != EmployeeActivityStatus.Approved || entity.PromotionId is not { } promotionId)
        {
            await db.SaveChangesAsync();
            return;
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        await criteriaTicks.SyncScoreAsync(promotionId);
        await transaction.CommitAsync();
    }

    private async Task<List<EmployeeActivityDto>> WithCreatorNames(List<EmployeeActivityDto> activities)
    {
        List<string> creatorIds = [.. activities.Select(a => a.CreatedById).OfType<string>().Distinct()];
        if (creatorIds.Count == 0) return activities;

        Dictionary<string, string> names;
        try
        {
            names = await employeeLookup.GetNamesByIdsAsync(creatorIds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not resolve activity creator names; showing records without them");
            return activities;
        }

        return [.. activities.Select(a => a with { CreatedByName = a.CreatedById is null ? null : names.GetValueOrDefault(a.CreatedById) })];
    }
}
