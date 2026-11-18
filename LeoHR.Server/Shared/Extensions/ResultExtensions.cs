using System.Net;
using System.Text.Json;

using FluentResults;

using LeoHR.Server.Shared.Results.Errors;

using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Shared.Extensions;

public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        HttpStatusCode statusCode = DetermineStatusCode(result);
        var errorResponse = result.ToErrorResponse();

        return new ObjectResult(errorResponse)
        {
            StatusCode = (int)statusCode
        };
    }

    public static ErrorResponse ToErrorResponse(this ResultBase result)
    {
        var errors = result.Errors
            .Select(error => new ErrorDetail
            {
                ErrorMessage = error.Message,
                PropertyName = error.Metadata.TryGetValue("PropertyName", out object? p) && p is string name
                    ? JsonNamingPolicy.CamelCase.ConvertName(name)
                    : null,
                ErrorCode = error.Metadata.TryGetValue("ErrorCode", out object? code) && code is string c ? c : null,
            })
            .ToList();

        if (errors.Count == 0)
            errors.Add(new ErrorDetail { ErrorMessage = "An error occurred." });

        return new ErrorResponse { Errors = errors };
    }

    public static ActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        HttpStatusCode statusCode = DetermineStatusCode(result);
        var errorResponse = result.ToErrorResponse();

        return new ObjectResult(errorResponse)
        {
            StatusCode = (int)statusCode
        };
    }

    private static HttpStatusCode DetermineStatusCode(ResultBase result)
    {
        return result.Errors.FirstOrDefault() switch
        {
            NotFoundError => HttpStatusCode.NotFound,
            ForbiddenError => HttpStatusCode.Forbidden,
            DuplicateResourceError => HttpStatusCode.Conflict,
            ServiceUnavailableError => HttpStatusCode.ServiceUnavailable,
            _ => HttpStatusCode.BadRequest
        };
    }
}
