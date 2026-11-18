using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.EmployeeActivities;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.EmployeeActivities;

public class PromotionActivitiesService(LeoHrDbContext db) : IPromotionActivities
{
    private const string ActivityResource = "Activity";
    private const string RemoveOthersActivity = "You can only remove activities you created.";
    private const string OnlyPlannedRemovable = "Only planned activities can be removed.";
    private const string ActivityAlreadyApproved = "Activity is already approved.";
    private const string PlannedNotApprovable = "A planned activity must be marked as done before it can be approved.";
    private const string OnlyApprovedRevertible = "Only approved activities can be reverted.";
    private const string CarriedOverNotRevertible = "An activity carried over from a previous promotion can't be reverted.";
    private const string FutureApprovalDate = "Date accomplished cannot be in the future for approved activities.";

    public async Task<List<EmployeeActivityEntity>> AddPlannedAsync(
        PromotionEntity promotion,
        IEnumerable<ActivityEntity> activities,
        string? note)
    {
        List<EmployeeActivityEntity> entities = [.. activities.Select(activity => new EmployeeActivityEntity
        {
            EmployeeId = promotion.EmployeeId,
            ActivityId = activity.Id,
            Activity = activity,
            PromotionId = promotion.Id,
            Status = EmployeeActivityStatus.Planned,
            Note = note,
        })];
        db.EmployeeActivities.AddRange(entities);
        await db.SaveChangesAsync();
        return entities;
    }

    public async Task<Result> RemovePlannedAsync(int promotionId, int employeeActivityId, ClaimsPrincipal user)
    {
        EmployeeActivityEntity? entity = await db.EmployeeActivities
            .FirstOrDefaultAsync(a => a.Id == employeeActivityId && a.PromotionId == promotionId);
        if (entity is null) return Result.Fail(new NotFoundError(ActivityResource, employeeActivityId));

        string? userId = user.GetUserId();
        if (!user.CanWriteManage() && entity.CreatedById != null && entity.CreatedById != userId)
            return Result.Fail(new ForbiddenError(RemoveOthersActivity));
        if (entity.Status != EmployeeActivityStatus.Planned)
            return Result.Fail(new BusinessRuleError(OnlyPlannedRemovable));

        db.EmployeeActivities.Remove(entity);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> ApproveAsync(
        string employeeId,
        int employeeActivityId,
        DateOnly accomplishedOn,
        string? approvedById,
        string? approvedByName)
    {
        EmployeeActivityEntity? entity = await db.EmployeeActivities
            .FirstOrDefaultAsync(a => a.Id == employeeActivityId && a.EmployeeId == employeeId);
        if (entity is null) return Result.Fail(new NotFoundError(ActivityResource, employeeActivityId));
        if (entity.Status == EmployeeActivityStatus.Approved)
            return Result.Fail(new BusinessRuleError(ActivityAlreadyApproved));
        if (entity.Status == EmployeeActivityStatus.Planned)
            return Result.Fail(new BusinessRuleError(PlannedNotApprovable));

        DateTime now = DateTime.UtcNow;
        if (accomplishedOn > DateOnly.FromDateTime(now))
            return Result.Fail(new BusinessRuleError(FutureApprovalDate));

        entity.Status = EmployeeActivityStatus.Approved;
        entity.AccomplishedOn = accomplishedOn;
        entity.PointsSnapshot ??= await db.Activities
            .AsNoTracking()
            .Where(a => a.Id == entity.ActivityId)
            .Select(a => (int?)a.Points)
            .FirstOrDefaultAsync() ?? 0;
        entity.ApprovedById = approvedById;
        entity.ApprovedByName = approvedByName;
        entity.ApprovedAt = now;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    // Back to pending; clearing the snapshot lets a later approval take the catalog's current points.
    public async Task<Result> RevertApprovalAsync(int promotionId, int employeeActivityId)
    {
        EmployeeActivityEntity? entity = await db.EmployeeActivities
            .FirstOrDefaultAsync(a => a.Id == employeeActivityId && a.PromotionId == promotionId);
        if (entity is null) return Result.Fail(new NotFoundError(ActivityResource, employeeActivityId));
        if (entity.Status != EmployeeActivityStatus.Approved)
            return Result.Fail(new BusinessRuleError(OnlyApprovedRevertible));
        if (entity.CarriedFromPromotionId is not null)
            return Result.Fail(new BusinessRuleError(CarriedOverNotRevertible));

        entity.Status = EmployeeActivityStatus.SelfReported;
        entity.PointsSnapshot = null;
        entity.ApprovedById = null;
        entity.ApprovedByName = null;
        entity.ApprovedAt = null;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task AddCarryOverAsync(
        PromotionEntity toPromotion,
        int fromPromotionId,
        IEnumerable<(EmployeeActivityEntity Source, int Points)> carried)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.EmployeeActivities.AddRange(carried.Select(c => new EmployeeActivityEntity
        {
            EmployeeId = toPromotion.EmployeeId,
            ActivityId = c.Source.ActivityId,
            PromotionId = toPromotion.Id,
            Status = EmployeeActivityStatus.Approved,
            AccomplishedOn = today,
            Note = c.Source.Note,
            PointsSnapshot = c.Points,
            ApprovedById = c.Source.ApprovedById,
            ApprovedByName = c.Source.ApprovedByName,
            ApprovedAt = c.Source.ApprovedAt,
            CarriedFromPromotionId = fromPromotionId,
        }));
        await db.SaveChangesAsync();
    }

    public async Task AdoptOrphanedAsync(PromotionEntity promotion)
    {
        List<EmployeeActivityEntity> pending = await db.EmployeeActivities
            .Where(a => a.EmployeeId == promotion.EmployeeId
                     && a.PromotionId != promotion.Id
                     && (
                         // pending activity from no promotion or a finished one — follows the employee forward
                         (a.Status == EmployeeActivityStatus.SelfReported
                             && (a.PromotionId == null
                                 || (a.Promotion!.Status != PromotionStatus.InProgress
                                     && a.Promotion!.Status != PromotionStatus.Ready)))
                         // activity approved before any promotion existed — attach it to the first one
                         // (a carry-over clone left orphaned by a deleted promotion must not be revived here)
                         || (a.Status == EmployeeActivityStatus.Approved
                             && a.PromotionId == null
                             && a.CarriedFromPromotionId == null)))
            .ToListAsync();

        foreach (EmployeeActivityEntity activity in pending)
        {
            if (activity.PromotionId is { } previousPromotionId && activity.CarriedFromPromotionId is null)
                activity.CarriedFromPromotionId = previousPromotionId;
            activity.PromotionId = promotion.Id;
        }

        await db.SaveChangesAsync();
    }

    // Every activity of promotions soft-deleted because the employee left, whatever its status.
    public Task DeleteAllForPromotionsAsync(IReadOnlyCollection<int> promotionIds)
    {
        return db.EmployeeActivities
            .Where(a => a.PromotionId != null && promotionIds.Contains(a.PromotionId.Value))
            .ExecuteDeleteAsync();
    }
}
