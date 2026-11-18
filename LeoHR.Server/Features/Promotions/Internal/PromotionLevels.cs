using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Promotions.Internal;

internal sealed class PromotionLevels(LeoHrDbContext db)
{
    public Task<List<string>> GetTargetLevels(PromotionEntity promotion)
    {
        return GetTargetLevels(promotion.EmployeeId, promotion.Technology, promotion.FromRole);
    }

    public async Task<List<string>> GetTargetLevels(string employeeId, Technology technology, string fromRole)
    {
        HashSet<int> reachedRanks = await GetReachedLevelRanks(employeeId);
        List<string> levelsAbove = SeniorityLadder.LevelsAbove(await GetActiveLevels(technology), fromRole);
        return [.. levelsAbove.Where(level => !reachedRanks.Contains(SeniorityLadder.Rank(level)))];
    }

    public async Task<Result> ValidateTargetLevel(
        string employeeId, Technology technology, string fromRole, string toRole, string toRoleField)
    {
        if (SeniorityLadder.Rank(toRole) <= SeniorityLadder.Rank(fromRole))
            return Result.Fail(new BusinessRuleError(PromotionMessages.LevelNotAvailable, toRoleField));

        if (!SeniorityLadder.LevelsAbove(await GetActiveLevels(technology), fromRole).Contains(toRole))
            return Result.Fail(new BusinessRuleError(PromotionMessages.NoCriteriaForLevel, toRoleField));

        if ((await GetReachedLevelRanks(employeeId)).Contains(SeniorityLadder.Rank(toRole)))
        {
            return Result.Fail(new BusinessRuleError(
                PromotionMessages.LevelAlreadyReached, toRoleField));
        }

        return Result.Ok();
    }

    // The Employee API can lag behind a promotion already sent from LeoHR, so start from the higher level.
    public async Task<string?> GetCurrentLevel(EmployeeRef emp)
    {
        string? lastPromotedTo = await db.Promotions
            .AsNoTracking()
            .Where(p => p.EmployeeId == emp.Id && p.Status == PromotionStatus.Sent)
            .OrderByDescending(p => p.ReferenceDate ?? p.CreatedAt)
            .Select(p => p.ToRole)
            .FirstOrDefaultAsync();
        return SeniorityLadder.Higher(EmployeeApiLevel(emp), lastPromotedTo);
    }

    public Task<List<PromotionLevel>> GetActiveLevels(Technology? technology = null)
    {
        return db.PromotionCriteria
            .AsNoTracking()
            .Where(c => c.IsActive && (technology == null || c.Technology == technology))
            .Select(c => new PromotionLevel(c.Technology, c.Seniority, c.SubLevel))
            .ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetLastPromotedToRoles()
    {
        var promoted = await db.Promotions
            .Where(p => p.Status == PromotionStatus.Sent)
            .Select(p => new { p.EmployeeId, p.ToRole, SentAt = p.ReferenceDate ?? p.CreatedAt })
            .ToListAsync();

        return promoted
            .GroupBy(p => p.EmployeeId)
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.SentAt)!.ToRole);
    }

    public static CreatePromotionRequest? BuildMissingPromotionRequest(
        EmployeeRef emp,
        string? lastPromotedTo,
        ILookup<Technology, PromotionLevel> levelsByTechnology,
        string defaultHolderId)
    {
        Technology? technology = TechnologyParser.TryParse(emp.Profile);
        string? fromRole = SeniorityLadder.Higher(EmployeeApiLevel(emp), lastPromotedTo);
        if (technology is null || fromRole is null) return null;

        string? toRole = SeniorityLadder.FindNextLevel(levelsByTechnology[technology.Value], fromRole);
        if (toRole is null) return null;

        string holderId = defaultHolderId == emp.Id ? string.Empty : defaultHolderId;
        return new CreatePromotionRequest(emp.Id, fromRole, toRole, technology.Value, holderId);
    }

    private async Task<HashSet<int>> GetReachedLevelRanks(string employeeId)
    {
        List<string> reachedRoles = await db.Promotions
            .AsNoTracking()
            .Where(p => p.EmployeeId == employeeId && p.Status == PromotionStatus.Sent)
            .Select(p => p.ToRole)
            .ToListAsync();
        return [.. reachedRoles.Select(SeniorityLadder.Rank)];
    }

    private static string EmployeeApiLevel(EmployeeRef emp)
    {
        return $"{emp.SenioritySubLevel} {emp.Seniority}";
    }
}
