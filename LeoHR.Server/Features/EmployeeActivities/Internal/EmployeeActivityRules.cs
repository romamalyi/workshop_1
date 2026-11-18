using System.Security.Claims;

using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Features.EmployeeActivities.Internal;

internal static class EmployeeActivityRules
{
    // Null when the token carries no client_id.
    public static string? IntegrationName(ClaimsPrincipal user)
    {
        string? clientId = user.GetClientId();
        return clientId is null ? null : IntegrationClients.NameFor(clientId);
    }

    public static void MarkApproved(
        EmployeeActivityEntity entity, ActivityEntity activity, DateOnly? accomplishedOn,
        string? approvedById, string? approvedByName)
    {
        entity.Status = EmployeeActivityStatus.Approved;
        entity.AccomplishedOn = accomplishedOn;
        entity.PointsSnapshot = activity.Points;
        entity.ApprovedById = approvedById;
        entity.ApprovedByName = approvedByName;
        entity.ApprovedAt = DateTime.UtcNow;
    }

    // Approved/SelfReported record something already done, so no future date; Planned is exempt (that's the plan).
    public static Result ValidateAccomplishedOn(EmployeeActivityStatus status, DateOnly? accomplishedOn)
    {
        bool mustBePast = status is EmployeeActivityStatus.Approved or EmployeeActivityStatus.SelfReported;
        return mustBePast && accomplishedOn > DateOnly.FromDateTime(DateTime.UtcNow)
            ? Result.Fail(new BusinessRuleError(EmployeeActivityMessages.FutureAccomplishedDate))
            : Result.Ok();
    }

    public static bool IsInSentPromotion(EmployeeActivityEntity activity)
    {
        return activity.Promotion?.Status == PromotionStatus.Sent;
    }
}
