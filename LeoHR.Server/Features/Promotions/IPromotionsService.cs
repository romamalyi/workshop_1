using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Models.Filter;

namespace LeoHR.Server.Features.Promotions;

public interface IPromotionsService
{
    Task<Result<PagedResult<PromotionDto>>> GetAll(ClaimsPrincipal user, PromotionFilterModel? filter = null);
    Task<Result<PromotionDto>> Create(CreatePromotionRequest request, ClaimsPrincipal user);
    Task<int> CreateMissingPromotions();
    Task<int> DeleteLeaversPromotions();
    Task<int> NotifyReadyPromotions();
    Task<Result<PromotionTargetLevelsDto>> GetTargetLevels(string employeeId);
    Task<Result<PromotionDetailDto>> GetDetailByPromotionId(int id, ClaimsPrincipal user);
    Task<Result<PromotionDetailDto>> GetDetailForCurrentUser(ClaimsPrincipal user);
    Task<Result<EmployeeLevelDto>> GetCurrentUserLevel(ClaimsPrincipal user);
    Task<Result<PromotionDetailDto?>> GetActiveDetailForEmployee(string employeeId, ClaimsPrincipal user);
    Task<Result<PromotionDetailDto?>> GetByCompanyId(string companyId, ClaimsPrincipal user);
    Task<Result> Update(int promotionId, UpdatePromotionRequest request);
    Task<Result<EmailRecipients>> GetEmailRecipients(int promotionId);
    Task<Result> SendPromotionEmail(int promotionId, SendPromotionRequest request, ClaimsPrincipal user);
    Task<Result<PromotionActivityDto>> AddActivity(int promotionId, AddActivityRequest request, ClaimsPrincipal user);
    Task<Result<AddActivitiesResult>> AddActivitiesToPromotion(int[] activityIds, int promotionId, ClaimsPrincipal user);
    Task<Result> RemoveActivity(int promotionId, int employeeActivityId, ClaimsPrincipal user);
    Task<Result> ApproveActivity(
        int promotionId, int employeeActivityId, DateOnly accomplishedOn, string? approvedById, ClaimsPrincipal user);
    Task<Result<List<PendingApprovalDto>>> GetPendingApprovals(List<string>? hrManagers = null);
    Task<Result> RevertActivityApproval(int promotionId, int employeeActivityId);
    Task<Result> SetCriterion(int promotionId, SetCriterionRequest request);
    Task<Result> VerifyCriteria(int promotionId, ClaimsPrincipal user);
}
