using System.Security.Claims;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionQueryExtensions
{
    public static IQueryable<PromotionEntity> ForUser(this IQueryable<PromotionEntity> query, ClaimsPrincipal user)
    {
        if (user.CanReadManage()) return query;
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return query.Where(_ => false);
        return query.Where(p => p.EmployeeId == userId);
    }

    public static IQueryable<PromotionEntity> WithStatus(
        this IQueryable<PromotionEntity> query,
        PromotionStatus? status)
    {
        return status switch
        {
            PromotionStatus.InProgress => query.Where(p => p.Status == PromotionStatus.InProgress),
            PromotionStatus.Ready => query.Where(p => p.Status == PromotionStatus.Ready),
            PromotionStatus.Sent => query.Where(p => p.Status == PromotionStatus.Sent),
            _ => query
        };
    }

    public static IQueryable<PromotionEntity> Open(this IQueryable<PromotionEntity> query)
    {
        return query.Where(p => p.Status == PromotionStatus.InProgress
                             || p.Status == PromotionStatus.Ready);
    }

    public static IQueryable<PromotionEntity> WithPendingActivities(
        this IQueryable<PromotionEntity> query,
        IQueryable<EmployeeActivityEntity> activities)
    {
        return query.Where(p => activities.Any(ea =>
            ea.PromotionId == p.Id &&
            ea.Status == EmployeeActivityStatus.SelfReported));
    }

    public static IQueryable<PromotionEntity> Paginate(
        this IQueryable<PromotionEntity> query,
        PromotionFilterModel? filter)
    {
        int pageNumber = filter?.PageNumber ?? 0;
        int pageSize = filter?.PageSize ?? 0;
        return pageNumber > 0 && pageSize > 0
            ? query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            : query;
    }

    public static IQueryable<PromotionRow> ProjectToRow(
        this IQueryable<PromotionEntity> query,
        IQueryable<EmployeeActivityEntity> activities)
    {
        return query.Select(p => new PromotionRow(
            p.Id,
            p.EmployeeId,
            p.FromRole,
            p.ToRole,
            p.Status,
            activities.Any(ea =>
                ea.PromotionId == p.Id && ea.Status == EmployeeActivityStatus.SelfReported),
            p.Status == PromotionStatus.Sent ? p.ReferenceDate ?? p.CreatedAt : null));
    }
}
