using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Results.Errors;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.AppSettings;

public interface IAppSettingsFeatureService
{
    Task<Result<EmployeeHistorySettingsDto>> GetEmployeeHistorySettings(ClaimsPrincipal user);
    Task<Result> SaveEmployeeHistorySettings(ClaimsPrincipal user, SaveEmployeeHistorySettingsRequest request);
    Task<Result<PromotionDefaultHolderDto>> GetPromotionDefaultHolder(ClaimsPrincipal user);
    Task<Result> SavePromotionDefaultHolder(ClaimsPrincipal user, SavePromotionDefaultHolderRequest request);
}

public class AppSettingsService(LeoHrDbContext db, IEmployeeLookup employeeLookup) : IAppSettingsFeatureService
{
    private const string MissingUserIdMessage = "The signed-in user has no id.";

    public async Task<Result<EmployeeHistorySettingsDto>> GetEmployeeHistorySettings(ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return Result.Fail(new ForbiddenError(MissingUserIdMessage));

        AppSettingsEntity entity = await GetOrCreateAsync(userId);
        return Result.Ok(new EmployeeHistorySettingsDto(
            entity.ShowManager,
            entity.ShowMentor,
            entity.ShowEmployment));
    }

    public async Task<Result> SaveEmployeeHistorySettings(ClaimsPrincipal user, SaveEmployeeHistorySettingsRequest request)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return Result.Fail(new ForbiddenError(MissingUserIdMessage));

        AppSettingsEntity entity = await GetOrCreateAsync(userId);
        entity.ShowManager = request.ShowManager;
        entity.ShowMentor = request.ShowMentor;
        entity.ShowEmployment = request.ShowEmployment;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<PromotionDefaultHolderDto>> GetPromotionDefaultHolder(ClaimsPrincipal user)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return Result.Fail(new ForbiddenError(MissingUserIdMessage));

        AppSettingsEntity entity = await GetOrCreateAsync(userId);
        return Result.Ok(new PromotionDefaultHolderDto(entity.PromotionDefaultHolder));
    }

    public async Task<Result> SavePromotionDefaultHolder(ClaimsPrincipal user, SavePromotionDefaultHolderRequest request)
    {
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(userId)) return Result.Fail(new ForbiddenError(MissingUserIdMessage));

        if (await employeeLookup.GetByIdAsync(request.EmployeeId) is null)
        {
            return Result.Fail(new NotFoundError(
                "Employee", request.EmployeeId, nameof(SavePromotionDefaultHolderRequest.EmployeeId)));
        }

        AppSettingsEntity entity = await GetOrCreateAsync(userId);
        entity.PromotionDefaultHolder = request.EmployeeId;
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<AppSettingsEntity> GetOrCreateAsync(string userId)
    {
        AppSettingsEntity? entity = await db.AppSettings.FindAsync(userId);
        if (entity is not null) return entity;

        entity = new AppSettingsEntity { UserId = userId };
        db.AppSettings.Add(entity);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            entity = await db.AppSettings.FindAsync(userId)
                ?? throw new InvalidOperationException($"AppSettings row for user '{userId}' not found after conflict.");
        }
        return entity;
    }
}
