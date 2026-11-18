using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionAccess
{
    public static async Task<Result> EnsureCanViewAsync(string employeeId, ClaimsPrincipal user, IEmployeeLookup employeeLookup)
    {
        return user.CanReadManage() || await employeeLookup.IsInManagementLineAsync(employeeId, user)
            ? Result.Ok()
            : Result.Fail(new ForbiddenError(PromotionMessages.NotYourPromotion));
    }

    public static Result EnsureCanModify(PromotionEntity promotion, ClaimsPrincipal user)
    {
        return user.CanWriteManage() || promotion.EmployeeId == user.GetUserId()
            ? Result.Ok()
            : Result.Fail(new ForbiddenError(PromotionMessages.NotYourPromotion));
    }
}
