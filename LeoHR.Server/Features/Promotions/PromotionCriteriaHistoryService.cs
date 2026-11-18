using LeoHR.Server.Features.Promotions.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Promotions;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Promotions;

public class PromotionCriteriaHistoryService(LeoHrDbContext db) : IPromotionCriteriaHistory
{
    public Task PreserveAsync(PromotionCriteriaEntity criteria)
    {
        return Link(criteria, includeUnsent: true);
    }

    public async Task<bool> PreserveSentAsync(PromotionCriteriaEntity criteria)
    {
        await Link(criteria, includeUnsent: false);
        return await db.Promotions.AnyAsync(p => p.PromotionCriteriaId == criteria.Id && p.Status == PromotionStatus.Sent);
    }

    private async Task Link(PromotionCriteriaEntity criteria, bool includeUnsent)
    {
        // Promotions read only an active criteria through its level.
        if (!criteria.IsActive) return;

        var unlinked = await db.Promotions
            .Where(p => p.PromotionCriteriaId == null && p.Technology == criteria.Technology
                && (includeUnsent || p.Status == PromotionStatus.Sent))
            .Select(p => new { p.Id, p.ToRole })
            .ToListAsync();
        List<int> ids = [.. unlinked
            .Where(p => SeniorityParser.TryParse(p.ToRole) == (criteria.Seniority, criteria.SubLevel))
            .Select(p => p.Id)];

        await db.Promotions
            .Where(p => ids.Contains(p.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PromotionCriteriaId, criteria.Id));
    }
}
