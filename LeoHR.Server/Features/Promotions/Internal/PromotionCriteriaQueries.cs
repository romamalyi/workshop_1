using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.PromotionCriteria;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionCriteriaQueries
{
    public static async Task<PromotionCriteriaDto?> ForPromotionAsync(
        this IQueryable<PromotionCriteriaEntity> criteria,
        PromotionEntity promotion)
    {
        PromotionCriteriaEntity? target = promotion.PromotionCriteriaId is { } criteriaId
            ? await criteria
                .AsNoTracking()
                .Include(c => c.RequiredActivity)
                .FirstOrDefaultAsync(c => c.Id == criteriaId)
            : await FindActiveAsync(criteria, promotion);
        if (target is null) return null;

        List<PromotionCriteriaDto> levels =
            [.. await GetIntermediateAsync(criteria, promotion), PromotionCriteriaDto.From(target)];
        return PromotionRules.MergeCriteria(levels);
    }

    public static Task<List<EmployeeActivityEntity>> ApprovedInPromotionAsync(
        this IQueryable<EmployeeActivityEntity> activities,
        int promotionId)
    {
        return activities
            .AsNoTracking()
            .Include(a => a.Activity)
            .Where(a => a.PromotionId == promotionId && a.Status == EmployeeActivityStatus.Approved)
            .ToListAsync();
    }

    // Levels strictly between FromRole and ToRole, lowest first: a promotion that skips them still owes their criteria.
    private static async Task<List<PromotionCriteriaDto>> GetIntermediateAsync(
        IQueryable<PromotionCriteriaEntity> criteria,
        PromotionEntity promotion)
    {
        int fromRank = SeniorityLadder.Rank(promotion.FromRole);
        int toRank = SeniorityLadder.Rank(promotion.ToRole);
        if (fromRank < 0) return [];

        List<PromotionCriteriaEntity> active = await criteria
            .AsNoTracking()
            .Include(c => c.RequiredActivity)
            .Where(c => c.IsActive && c.Technology == promotion.Technology)
            .ToListAsync();
        return [.. active
            .Select(c => (Rank: SeniorityLadder.Rank(c.Seniority, c.SubLevel), Criteria: c))
            .Where(x => x.Rank > fromRank && x.Rank < toRank)
            .OrderBy(x => x.Rank)
            .Select(x => PromotionCriteriaDto.From(x.Criteria))];
    }

    private static async Task<PromotionCriteriaEntity?> FindActiveAsync(
        IQueryable<PromotionCriteriaEntity> criteria,
        PromotionEntity promotion)
    {
        (Seniority? seniority, SubLevel subLevel) = SeniorityParser.TryParse(promotion.ToRole);
        if (seniority is null) return null;

        return await criteria
            .AsNoTracking()
            .Include(c => c.RequiredActivity)
            .FirstOrDefaultAsync(c => c.IsActive
                && c.Technology == promotion.Technology && c.Seniority == seniority.Value && c.SubLevel == subLevel);
    }
}
