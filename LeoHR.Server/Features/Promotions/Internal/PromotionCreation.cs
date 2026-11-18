using System.Security.Claims;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.EmployeeActivities;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.PromotionCriteria;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeoHR.Server.Features.Promotions.Internal;

internal sealed class PromotionCreation(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IPromotionActivities promotionActivities,
    PromotionCriteriaTicksService criteriaTicks,
    ILogger logger)
{
    private readonly PromotionData data = new(db, employeeLookup);
    private readonly PromotionLevels promotionLevels = new(db);

    public async Task<PromotionEntity> Create(CreatePromotionRequest request, EmployeeRef? emp)
    {
        DateTime? levelStartDate = await data.GetLevelStartDate(request.EmployeeId);
        PromotionEntity promotion = PromotionMapper.BuildPromotion(request, levelStartDate);
        db.Promotions.Add(promotion);

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await db.SaveChangesAsync();
        await CarryOverSurplusPoints(promotion);
        await promotionActivities.AdoptOrphanedAsync(promotion);

        if (await criteriaTicks.SyncAutoCriteria(promotion, emp)) await db.SaveChangesAsync();

        await transaction.CommitAsync();
        return promotion;
    }

    public async Task CreateNext(PromotionEntity sent, EmployeeRef? emp, ClaimsPrincipal user)
    {
        if (emp is null) return;

        try
        {
            if (await data.HasOpenPromotion(sent.EmployeeId)) return;

            Technology? technology = TechnologyParser.TryParse(emp.Profile);
            if (technology is null) return;

            List<PromotionLevel> levels = await promotionLevels.GetActiveLevels(technology);
            string? toRole = SeniorityLadder.FindNextLevel(levels, sent.ToRole);
            if (toRole is null) return;

            string holderId = await data.GetDefaultHolderId(user, sent.EmployeeId);
            CreatePromotionRequest request = new(sent.EmployeeId, sent.ToRole, toRole, technology.Value, holderId);
            await Create(request, emp);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(LeoHrDbContext.OpenPromotionIndex))
        {
            // Another request created it first.
        }
        catch (Exception ex)
        {
            // The send is already saved (and the email sent); MissingPromotionsJob creates it on its next run.
            logger.LogError(ex, "Creating the promotion after {PromotionId} failed", sent.Id);
        }
    }

    private async Task CarryOverSurplusPoints(PromotionEntity newPromotion)
    {
        PromotionEntity? previous = await data.GetLastSuccessfulPromotion(newPromotion);
        if (previous is null) return;

        PromotionCriteriaDto? previousCriteria = await db.PromotionCriteria.ForPromotionAsync(previous);
        if (previousCriteria?.ActivitiesScore is not { } requiredScore) return;

        List<EmployeeActivityEntity> approved = await db.EmployeeActivities.ApprovedInPromotionAsync(previous.Id);
        if (approved.Count == 0) return;

        // Measure the previous promotion's surplus against the same window boundaries its own
        // detail view uses (its reference date and its snapshotted level start), so the carried
        // amount matches what that promotion shows as achieved.
        DateTime refDate = previous.ReferenceDate ?? previous.CreatedAt;
        List<EmployeeActivityEntity> validApproved =
            [.. PromotionRules.GetValidApprovedActivities(approved, refDate, previous.LevelStartDate)];
        int surplus = validApproved.Sum(a => a.PointsSnapshot ?? 0) - requiredScore;
        if (surplus <= 0) return;

        List<int> requiredActivityIds = [.. (await db.PromotionCriteria.ForPromotionAsync(newPromotion))?.RequiredActivities.Select(a => a.Id) ?? []];

        List<(EmployeeActivityEntity Source, int Points)> carried =
            CarryOverCalculator.SelectCarryOver(surplus, validApproved, requiredActivityIds);
        if (carried.Count == 0) return;

        await promotionActivities.AddCarryOverAsync(newPromotion, previous.Id, carried);
    }
}
