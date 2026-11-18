using System.Security.Claims;

using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LeoHR.Server.Shared.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class OwnerOrManagerAttribute(string routeParam = "employeeId") : Attribute, IAuthorizationFilter
{
    // Read-only actions also let LeoHRManage Read through.
    public bool ReadOnly { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ClaimsPrincipal user = context.HttpContext.User;

        if (ReadOnly ? user.CanReadManage() : user.CanWriteManage())
        {
            return;
        }

        string? resourceEmployeeId = context.RouteData.Values[routeParam]?.ToString();
        string? userId = user.GetUserId();
        if (string.IsNullOrEmpty(resourceEmployeeId) || string.IsNullOrEmpty(userId) ||
            resourceEmployeeId != userId)
        {
            context.Result = new ForbidResult();
        }
    }
}
