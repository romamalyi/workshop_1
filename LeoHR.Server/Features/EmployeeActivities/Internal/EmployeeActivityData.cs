using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.EmployeeActivities.Internal;

internal sealed class EmployeeActivityData(LeoHrDbContext db)
{
    private static readonly ActivityCategory[] integrationCategories =
        [ActivityCategory.SelfCourses, ActivityCategory.Certifications, ActivityCategory.RnD];

    // ApprovedByName holds the name of the integration that recorded the activity and marks its owner.
    public IQueryable<EmployeeActivityEntity> IntegrationOwned(string integrationName)
    {
        return db.EmployeeActivities.Where(x => x.ApprovedById == null
            && x.ApprovedByName == integrationName
            && x.CarriedFromPromotionId == null);
    }

    public IQueryable<ActivityEntity> IntegrationActivities()
    {
        return db.Activities.Where(a => a.IsActive && integrationCategories.Contains(a.Category));
    }

    // Standalone activities only: excludes carry-over clones (CarriedFromPromotionId != null), which are
    // promotion-derived and not independently readable or editable.
    public IQueryable<EmployeeActivityEntity> FindUserActivity(string employeeId, int id)
    {
        return db.EmployeeActivities.Where(x => x.Id == id
            && x.EmployeeId == employeeId
            && x.CarriedFromPromotionId == null);
    }

    public Task<bool> IsEmployeePromotion(string employeeId, int promotionId)
    {
        return db.Promotions.AsNoTracking().AnyAsync(p => p.Id == promotionId && p.EmployeeId == employeeId);
    }

    public Task<bool> IsSentPromotion(int promotionId)
    {
        return db.Promotions.AsNoTracking().AnyAsync(p => p.Id == promotionId && p.Status == PromotionStatus.Sent);
    }

    // A sent promotion is read-only, so a new activity doesn't join it; it waits for the next promotion to adopt it.
    public Task<int?> ResolveActivePromotionId(string employeeId)
    {
        return db.Promotions
            .Where(p => p.EmployeeId == employeeId && p.Status != PromotionStatus.Sent)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Result> ValidateDuplicateActivityAsync(string employeeId, ActivityEntity activity)
    {
        if (activity.WithinPeriod == ActivityPeriod.OverallActive)
        {
            bool isDuplicate = await db.EmployeeActivities.AnyAsync(x =>
                x.EmployeeId == employeeId &&
                x.ActivityId == activity.Id &&
                x.CarriedFromPromotionId == null);

            if (isDuplicate)
            {
                return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.OverallActiveSubmittedOnce));
            }
        }
        else if (activity.WithinPeriod == ActivityPeriod.OverallInLevel)
        {
            DateTime? levelStartDate = await db.EmployeeExtensions
                .AsNoTracking()
                .Where(x => x.EmployeeId == employeeId)
                .Select(x => x.LevelStartDate)
                .FirstOrDefaultAsync();
            bool isDuplicate = await db.EmployeeActivities.AnyAsync(x =>
                x.EmployeeId == employeeId &&
                x.ActivityId == activity.Id &&
                x.CarriedFromPromotionId == null &&
                (levelStartDate == null || x.CreatedAt >= levelStartDate));

            if (isDuplicate)
            {
                return Result.Fail(new BusinessRuleError(EmployeeActivityMessages.AlreadyCompletedInLevel));
            }
        }

        return Result.Ok();
    }
}
