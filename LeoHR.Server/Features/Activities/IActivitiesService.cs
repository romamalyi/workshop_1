using FluentResults;

using LeoHR.Server.Shared;

namespace LeoHR.Server.Features.Activities;

public interface IActivitiesService
{
    Task<List<ActivityDto>> GetAll(string? category, bool includeInactive);
    Task<List<CategorizedActivity>> GetGroupedActivities();
    Task<Result<ActivityDto>> GetById(int id);
    Task<Result<ActivityDto>> Create(CreateActivityRequest request);
    Task<Result<ActivityDto>> Update(int id, UpdateActivityRequest request, EditMode mode);
    Task<Result<ActivityDto>> Reactivate(int id);
    Task<Result> Delete(int id);
}
