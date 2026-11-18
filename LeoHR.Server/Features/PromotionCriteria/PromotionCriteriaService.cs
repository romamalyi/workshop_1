using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.PromotionCriteria;
using LeoHR.Server.Shared.Promotions;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LeoHR.Server.Features.PromotionCriteria;

public class PromotionCriteriaService(LeoHrDbContext db, IPromotionCriteriaHistory criteriaHistory) : IPromotionCriteriaService
{
    private const string CriteriaResource = "Promotion criteria";
    private const string DuplicateMessage = "A criteria for this level already exists on this technology.";
    private const string DuplicateActiveMessage = "An active criteria for this level already exists on this technology.";
    private const string InvalidRequiredActivityMessage =
        "The required achievement must be an active activity available for this technology.";

    public async Task<List<PromotionCriteriaDto>> GetAll(string? technology, bool includeInactive)
    {
        IQueryable<PromotionCriteriaEntity> query = db.PromotionCriteria.Include(x => x.RequiredActivity).AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrEmpty(technology) && Enum.TryParse<Technology>(technology, out Technology tech))
            query = query.Where(x => x.Technology == tech);

        List<PromotionCriteriaEntity> entities = await query.ToListAsync();

        IOrderedEnumerable<PromotionCriteriaEntity> sortedEntities = entities
            .OrderBy(x => x.Technology)
            .ThenBy(x => (int)x.Seniority)
            .ThenBy(x => (int)x.SubLevel);

        return [.. sortedEntities.Select(PromotionCriteriaDto.From)];
    }

    public async Task<Result<PromotionCriteriaDto>> GetById(int id)
    {
        PromotionCriteriaEntity? entity = await db.PromotionCriteria
            .Include(x => x.RequiredActivity)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity is null
            ? Result.Fail(new NotFoundError(CriteriaResource, id))
            : PromotionCriteriaDto.From(entity);
    }

    public async Task<Result<PromotionCriteriaDto>> Create(PromotionCriteriaRequest request)
    {
        Technology tech = request.Technology;
        Seniority seniority = request.Seniority;
        SubLevel subLevel = request.SubLevel;

        bool duplicate = await db.PromotionCriteria
            .AnyAsync(x => x.IsActive && x.Technology == tech && x.Seniority == seniority && x.SubLevel == subLevel);
        if (duplicate) return Result.Fail(new DuplicateResourceError(DuplicateMessage));

        Result<ActivityEntity?> required = await ResolveRequiredActivity(request.RequiredActivityId, request.Technology);
        if (required.IsFailed) return Result.Fail<PromotionCriteriaDto>(required.Errors);

        DateTime now = DateTime.UtcNow;
        var entity = new PromotionCriteriaEntity { CreatedAt = now, IsActive = true };
        ApplyPromotionCriteriaFields(entity, request, now);
        entity.RequiredActivity = required.Value;

        db.PromotionCriteria.Add(entity);
        await db.SaveChangesAsync();
        return PromotionCriteriaDto.From(entity);
    }

    public async Task<Result<PromotionCriteriaDto>> Update(int id, PromotionCriteriaRequest request, EditMode mode)
    {
        PromotionCriteriaEntity? existing = await db.PromotionCriteria.FindAsync(id);
        if (existing is null) return Result.Fail(new NotFoundError(CriteriaResource, id));

        Result<ActivityEntity?> required = await ResolveRequiredActivity(request.RequiredActivityId, request.Technology);
        if (required.IsFailed) return Result.Fail<PromotionCriteriaDto>(required.Errors);

        DateTime now = DateTime.UtcNow;

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();

        if (mode == EditMode.Hard && !await criteriaHistory.PreserveSentAsync(existing))
        {
            ApplyPromotionCriteriaFields(existing, request, now);
            existing.RequiredActivity = required.Value;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return PromotionCriteriaDto.From(existing);
        }

        Technology tech = request.Technology;
        Seniority seniority = request.Seniority;
        SubLevel subLevel = request.SubLevel;

        bool duplicate = await db.PromotionCriteria.AnyAsync(x =>
            x.Id != id && x.IsActive && x.Technology == tech && x.Seniority == seniority && x.SubLevel == subLevel);
        if (duplicate) return Result.Fail(new DuplicateResourceError(DuplicateMessage));

        if (mode == EditMode.Soft) await criteriaHistory.PreserveAsync(existing);

        existing.IsActive = false;
        existing.UpdatedAt = now;

        var newEntity = new PromotionCriteriaEntity { CreatedAt = now, IsActive = true };
        ApplyPromotionCriteriaFields(newEntity, request, now);
        newEntity.RequiredActivity = required.Value;

        db.PromotionCriteria.Add(newEntity);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return PromotionCriteriaDto.From(newEntity);
    }

    public async Task<Result<PromotionCriteriaDto>> Reactivate(int id)
    {
        PromotionCriteriaEntity? entity = await db.PromotionCriteria
            .Include(x => x.RequiredActivity)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return Result.Fail(new NotFoundError(CriteriaResource, id));

        bool duplicate = await db.PromotionCriteria.AnyAsync(x =>
            x.Id != id && x.IsActive &&
            x.Technology == entity.Technology &&
            x.Seniority == entity.Seniority &&
            x.SubLevel == entity.SubLevel);
        if (duplicate) return Result.Fail(new DuplicateResourceError(DuplicateActiveMessage));

        entity.IsActive = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return PromotionCriteriaDto.From(entity);
    }

    public async Task<Result> Delete(int id)
    {
        PromotionCriteriaEntity? entity = await db.PromotionCriteria.FindAsync(id);
        if (entity is null) return Result.Fail(new NotFoundError(CriteriaResource, id));

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
        await criteriaHistory.PreserveAsync(entity);
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Ok();
    }

    private async Task<Result<ActivityEntity?>> ResolveRequiredActivity(int? requiredActivityId, Technology technology)
    {
        if (requiredActivityId is null) return Result.Ok<ActivityEntity?>(null);

        ActivityEntity? activity = await db.Activities.FirstOrDefaultAsync(a =>
            a.Id == requiredActivityId && a.IsActive && (a.Technology == null || a.Technology == technology));

        return activity is null
            ? Result.Fail<ActivityEntity?>(new BusinessRuleError(InvalidRequiredActivityMessage))
            : Result.Ok<ActivityEntity?>(activity);
    }

    private static void ApplyPromotionCriteriaFields(
        PromotionCriteriaEntity entity,
        PromotionCriteriaRequest input,
        DateTime now)
    {
        entity.Technology = input.Technology;
        entity.Seniority = input.Seniority;
        entity.SubLevel = input.SubLevel;
        entity.WorkExperienceFromMonths = input.WorkExperienceFromMonths;
        entity.WorkExperienceToMonths = input.WorkExperienceToMonths;
        entity.RequiredExperience = input.RequiredExperience;
        entity.EnglishLevel = input.EnglishLevel;
        entity.RequiredActivityId = input.RequiredActivityId;
        entity.KeRequired = input.KeRequired;
        entity.ActivitiesScore = input.ActivitiesScore;
        entity.UpdatedAt = now;
    }
}
