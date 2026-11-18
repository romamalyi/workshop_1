using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.EmployeeActivities;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeoHR.Server.Features.Activities;

public class ActivitiesService(LeoHrDbContext db, IActivityVersions activityVersions) : IActivitiesService
{
    public const string Activity = "Activity";
    public const string InUseByCriteriaMessage = "This activity is required by one or more promotion criteria and cannot be deactivated. Update those criteria first.";

    public async Task<List<ActivityDto>> GetAll(string? category, bool includeInactive)
    {
        IQueryable<ActivityEntity> query = db.Activities.AsQueryable();

        if (!includeInactive)
            query = query.Where(a => a.IsActive);

        if (!string.IsNullOrEmpty(category) && Enum.TryParse<ActivityCategory>(category, out ActivityCategory cat))
            query = query.Where(a => a.Category == cat);

        List<ActivityEntity> activities = await query
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Id)
            .ToListAsync();

        HashSet<int> requirementIds = await GetRequirementActivityIds();
        return [.. activities.Select(a => MapToDto(a, requirementIds.Contains(a.Id)))];
    }

    public async Task<List<CategorizedActivity>> GetGroupedActivities()
    {
        List<ActivityEntity> activities = await db.Activities
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Id)
            .ToListAsync();

        HashSet<int> requirementIds = await GetRequirementActivityIds();
        return [.. activities
            .GroupBy(a => a.Category.ToString())
            .Select(g => new CategorizedActivity(g.Key, [.. g.Select(a => MapToDto(a, requirementIds.Contains(a.Id)))]))];
    }

    public async Task<Result<ActivityDto>> GetById(int id)
    {
        ActivityEntity? entity = await db.Activities.FindAsync(id);
        if (entity is null) return Result.Fail(new NotFoundError(Activity, id));

        return MapToDto(entity, await IsUsedByCriteria(id));
    }

    public async Task<Result<ActivityDto>> Create(CreateActivityRequest request)
    {
        if (await NameTaken(request.Name, excludeId: null))
            return Result.Fail(DuplicateNameError(request.Name));

        DateTime now = DateTime.UtcNow;
        var entity = new ActivityEntity
        {
            Category = request.Category,
            Name = request.Name.Trim(),
            WithinPeriod = request.WithinPeriod,
            Technology = request.Technology,
            Points = request.Points,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Activities.Add(entity);
        await db.SaveChangesAsync();
        return MapToDto(entity, isPromotionRequirement: false);
    }

    public async Task<Result<ActivityDto>> Update(int id, UpdateActivityRequest request, EditMode mode)
    {
        ActivityEntity? existing = await db.Activities.FindAsync(id);
        if (existing is null) return Result.Fail(new NotFoundError(Activity, id));

        if (await NameTaken(request.Name, excludeId: id))
            return Result.Fail(DuplicateNameError(request.Name));

        DateTime now = DateTime.UtcNow;

        // Hard edit updates the existing row, so FK references remain valid.
        if (mode == EditMode.Hard)
        {
            existing.Category = request.Category;
            existing.Name = request.Name.Trim();
            existing.WithinPeriod = request.WithinPeriod;
            existing.Technology = request.Technology;
            existing.Points = request.Points;
            existing.UpdatedAt = now;
            await db.SaveChangesAsync();
            return MapToDto(existing, await IsUsedByCriteria(id));
        }

        if (await IsUsedByCriteria(id))
            return Result.Fail(new BusinessRuleError(InUseByCriteriaMessage));

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        existing.IsActive = false;
        existing.UpdatedAt = now;

        var newEntity = new ActivityEntity
        {
            Category = request.Category,
            Name = request.Name.Trim(),
            WithinPeriod = request.WithinPeriod,
            Technology = request.Technology,
            Points = request.Points,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Activities.Add(newEntity);
        await db.SaveChangesAsync();
        await activityVersions.MoveOpenRecordsAsync(existing.Id, newEntity.Id);
        await transaction.CommitAsync();
        return MapToDto(newEntity, isPromotionRequirement: false);
    }

    public async Task<Result<ActivityDto>> Reactivate(int id)
    {
        ActivityEntity? entity = await db.Activities.FindAsync(id);
        if (entity is null) return Result.Fail(new NotFoundError(Activity, id));

        if (await NameTaken(entity.Name, excludeId: id))
            return Result.Fail(DuplicateNameError(entity.Name));

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return MapToDto(entity, await IsUsedByCriteria(id));
    }

    public async Task<Result> Delete(int id)
    {
        ActivityEntity? entity = await db.Activities.FindAsync(id);
        if (entity is null) return Result.Fail(new NotFoundError(Activity, id));

        if (await IsUsedByCriteria(id))
            return Result.Fail(new BusinessRuleError(InUseByCriteriaMessage));

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    private Task<bool> NameTaken(string name, int? excludeId)
    {
        string normalized = name.Trim().ToLower();
        return db.Activities.AnyAsync(a =>
            a.IsActive && a.Id != excludeId && a.Name.Trim().ToLower() == normalized);
    }

    private static DuplicateResourceError DuplicateNameError(string name)
    {
        return new($"An activity named '{name.Trim()}' already exists.");
    }

    private Task<bool> IsUsedByCriteria(int activityId)
    {
        return db.PromotionCriteria.AnyAsync(c => c.IsActive && c.RequiredActivityId == activityId);
    }

    private async Task<HashSet<int>> GetRequirementActivityIds()
    {
        List<int> ids = await db.PromotionCriteria
            .Where(c => c.IsActive && c.RequiredActivityId != null)
            .Select(c => c.RequiredActivityId!.Value)
            .Distinct()
            .ToListAsync();
        return [.. ids];
    }

    private static ActivityDto MapToDto(ActivityEntity a, bool isPromotionRequirement)
    {
        return new(a.Id, a.Category.ToString(), a.Name, a.WithinPeriod.ToString(), a.Points, a.IsActive,
            a.Technology?.ToString(), isPromotionRequirement);
    }
}
