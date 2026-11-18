using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Promotions;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Promotions.Internal;

internal sealed class PromotionData(LeoHrDbContext db, IEmployeeLookup employeeLookup)
{
    private const int MaxPendingApprovals = 1000;

    public async Task<List<PendingApprovalDto>> LoadPendingApprovals(List<string> activeEmployeeIds)
    {
        var rows = await db.EmployeeActivities
            .Where(a => a.Status == EmployeeActivityStatus.SelfReported
                     && activeEmployeeIds.Contains(a.EmployeeId))
            .OrderBy(a => a.CreatedAt)
            .Take(MaxPendingApprovals)
            .Select(a => new
            {
                a.Id,
                a.EmployeeId,
                a.PromotionId,
                FromRole = a.Promotion != null ? a.Promotion.FromRole : null,
                ToRole = a.Promotion != null ? a.Promotion.ToRole : null,
                a.Activity.Category,
                a.Activity.Name,
                a.Activity.WithinPeriod,
                Points = a.PointsSnapshot ?? a.Activity.Points,
                a.Status,
                a.AccomplishedOn,
                a.Note,
            })
            .ToListAsync();

        List<EmployeeRef> employees =
            await employeeLookup.GetByIdsAsync([.. rows.Select(r => r.EmployeeId).Distinct()]);
        var byId = employees.ToDictionary(e => e.Id);

        return [.. rows.Select(r =>
        {
            byId.TryGetValue(r.EmployeeId, out EmployeeRef? emp);
            string name = emp is null ? string.Empty : $"{emp.FirstName} {emp.LastName}".Trim();
            return new PendingApprovalDto(
                EmployeeActivityId: r.Id,
                EmployeeId: r.EmployeeId,
                PromotionId: r.PromotionId,
                EmployeeName: name,
                FromRole: r.FromRole,
                ToRole: r.ToRole,
                Category: r.Category.ToString(),
                Name: r.Name,
                WithinPeriod: r.WithinPeriod.ToString(),
                PointsSnapshot: r.Points,
                Status: r.Status,
                AccomplishedOn: r.AccomplishedOn,
                Note: r.Note);
        })];
    }

    public async Task<string> GetDefaultHolderId(ClaimsPrincipal user, string promotedEmployeeId)
    {
        string userId = user.GetUserId() ?? string.Empty;
        string? savedHolderId = await db.AppSettings
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.PromotionDefaultHolder)
            .FirstOrDefaultAsync();

        // Either holder may have left since; a dangling id would be stored and then dropped from the email.
        string holderId = string.Empty;
        if (savedHolderId is not null && await IsEmployee(savedHolderId))
        {
            holderId = savedHolderId;
        }
        else if (await IsEmployee(AppSettingsEntity.DefaultPromotionHolder))
        {
            holderId = AppSettingsEntity.DefaultPromotionHolder;
        }

        return holderId == promotedEmployeeId ? string.Empty : holderId;
    }

    public Task<Dictionary<RecipientRole, Recipient>> GetHolderRole(PromotionEntity promotion)
    {
        return employeeLookup.GetPromotionHolderRoleAsync(promotion.PromotionHolderId);
    }

    public async Task<Result<List<Recipient>>> FindExtraCc(List<string>? employeeIds)
    {
        if (employeeIds is not { Count: > 0 }) return new List<Recipient>();

        List<EmployeeRef> employees =
            [.. (await employeeLookup.GetByIdsAsync(employeeIds)).Where(e => !string.IsNullOrWhiteSpace(e.Email))];
        if (employees.Count != employeeIds.Distinct().Count())
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.ExtraCcNotFound, nameof(SendPromotionRequest.ExtraCcEmployeeIds)));
        }

        return employees.Select(e => new Recipient(PromotionMapper.FullName(e), e.Email)).ToList();
    }

    public async Task<List<string>> GetActiveEmployeeIds()
    {
        return [.. (await employeeLookup.GetAllActiveAsync()).Select(e => e.Id)];
    }

    public async Task<List<string>> FilterByHrManagers(List<string> employeeIds, List<string>? hrManagers)
    {
        if (hrManagers is not { Count: > 0 }) return employeeIds;
        return [.. (await employeeLookup.GetByIdsAsync(employeeIds)).WithHrManagers(hrManagers).Select(e => e.Id)];
    }

    public Task<PromotionEntity?> LoadOpenPromotion(string employeeId)
    {
        return db.Promotions.Open().FirstOrDefaultAsync(p => p.EmployeeId == employeeId);
    }

    public Task<bool> HasOpenPromotion(string employeeId)
    {
        return db.Promotions.Open().AnyAsync(p => p.EmployeeId == employeeId);
    }

    public Task<bool> IsActivityAlreadyAdded(int promotionId, int activityId)
    {
        return db.EmployeeActivities.AnyAsync(ea => ea.PromotionId == promotionId && ea.ActivityId == activityId);
    }

    public Task<List<EmployeeActivityEntity>> LoadPromotionActivities(PromotionEntity promotion)
    {
        return db.EmployeeActivities
            .AsNoTracking()
            .Include(a => a.Activity)
            .Where(a => a.EmployeeId == promotion.EmployeeId && a.PromotionId == promotion.Id)
            .ToListAsync();
    }

    public async Task<List<PromotionHistoryItem>> LoadHistory(PromotionEntity promotion)
    {
        var sent = await db.Promotions
            .Where(p => p.EmployeeId == promotion.EmployeeId
                        && p.Id != promotion.Id
                        && p.Status == PromotionStatus.Sent)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.Id, p.FromRole, p.ToRole, p.Status, p.CreatedAt })
            .ToListAsync();

        return [.. sent
            .Select(p =>
                new PromotionHistoryItem(p.Id, p.FromRole, p.ToRole, p.Status, p.CreatedAt))];
    }

    public Task<PromotionEntity?> GetLastSuccessfulPromotion(PromotionEntity newPromotion)
    {
        return db.Promotions
            .Where(p => p.EmployeeId == newPromotion.EmployeeId
                     && p.Id != newPromotion.Id
                     && p.Status == PromotionStatus.Sent)
            .OrderByDescending(p => p.ReferenceDate ?? p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<DateTime?> GetLevelStartDate(string employeeId)
    {
        return db.EmployeeExtensions
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .Select(x => x.LevelStartDate)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsEmployee(string employeeId)
    {
        return !string.IsNullOrWhiteSpace(employeeId) && await employeeLookup.GetByIdAsync(employeeId) is not null;
    }
}
