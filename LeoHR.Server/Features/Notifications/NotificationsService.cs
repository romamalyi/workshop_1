using System.Net;

using FluentResults;

using LeoHR.Server.Features.Notifications.Dto;
using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.Features.Notifications;

public class NotificationsService(
    IEmployeeLookup employeeLookup,
    IEmailService emailService,
    ILogger<NotificationsService> logger) : INotificationsService
{
    private const int ParameterMaxLength = 4000;

    public async Task<Result> HandleEmployeeEvent(EmployeeEventRequest request)
    {
        EmployeeRef? employee = await employeeLookup.GetByIdAsync(request.EmployeeId);
        if (employee is null)
        {
            logger.LogWarning("Employee event {EventType} rejected: employee {EmployeeId} not found",
                request.EventType, request.EmployeeId);
            return Result.Fail(new NotFoundError("Employee", request.EmployeeId));
        }

        return request.EventType switch
        {
            EmployeeEventType.EnglishGroupAssigned => await SendEnglishGroupAssigned(request, employee),
            EmployeeEventType.EnglishLevelChanged => await SendEnglishLevelChanged(request, employee),
            _ => await NotifyHr(request, employee),
        };
    }

    private async Task<Result> NotifyHr(EmployeeEventRequest request, EmployeeRef employee)
    {
        var parameters = new Dictionary<string, string>
        {
            ["employeeName"] = FullName(employee),
            ["eventType"] = request.EventType!.Value.ToString(),
            ["eventDescription"] = request.EventDescription,
        };

        return await Send(request, employee, EmailTemplateCatalog.EmployeeEvent, parameters);
    }

    private async Task<Result> SendEnglishLevelChanged(EmployeeEventRequest request, EmployeeRef employee)
    {
        Result valid = ValidateParameters(request, required: ["currentLevel"]);
        if (valid.IsFailed) return valid;

        var parameters = new Dictionary<string, string>
        {
            ["employeeName"] = Html(FullName(employee)),
            ["currentLevel"] = Level(request.Parameters, "currentLevel"),
            ["previousLevel"] = Level(request.Parameters, "previousLevel"),
            ["strengths"] = HtmlList(request.Parameters, "strengths"),
            ["improvementAreas"] = HtmlList(request.Parameters, "improvementAreas"),
        };

        EmployeeRef? teacher = await FindTeacher(request);
        return await Send(request, employee, EmailTemplateCatalog.EnglishLevelChanged, parameters, teacher);
    }

    private async Task<Result> SendEnglishGroupAssigned(EmployeeEventRequest request, EmployeeRef employee)
    {
        Result valid = ValidateParameters(request, required: ["groupName", "schedule", "sessionsPerWeek"]);
        if (valid.IsFailed) return valid;

        EmployeeRef? teacher = await FindTeacher(request);
        var parameters = new Dictionary<string, string>
        {
            ["employeeName"] = Html(FullName(employee)),
            ["groupName"] = Html(request.Parameters!["groupName"]),
            ["schedule"] = Html(request.Parameters["schedule"]),
            ["sessionsPerWeek"] = Html(request.Parameters["sessionsPerWeek"]),
            ["teacherName"] = teacher is null ? "TBD" : Html(FullName(teacher)),
        };

        return await Send(request, employee, EmailTemplateCatalog.EnglishGroupAssigned, parameters, teacher);
    }

    // The teacher is optional: an unknown one only costs the email its teacher sender and recipient.
    private async Task<EmployeeRef?> FindTeacher(EmployeeEventRequest request)
    {
        string? teacherId = request.Parameters?.GetValueOrDefault("teacherId");
        if (string.IsNullOrWhiteSpace(teacherId)) return null;

        EmployeeRef? teacher = await employeeLookup.GetByIdAsync(teacherId);
        if (teacher is null)
        {
            logger.LogWarning("Employee event {EventType} for employee {EmployeeId}: teacher {TeacherId} not found",
                request.EventType, request.EmployeeId, teacherId);
        }

        return teacher;
    }

    private Result ValidateParameters(EmployeeEventRequest request, string[] required)
    {
        Dictionary<string, string> parameters = request.Parameters ?? [];

        string? missing = required.FirstOrDefault(key => string.IsNullOrWhiteSpace(parameters.GetValueOrDefault(key)));
        if (missing is not null)
            return Result.Fail(new BusinessRuleError($"Parameter '{missing}' is required for {request.EventType}.", nameof(request.Parameters)));

        string? tooLong = parameters.Keys.FirstOrDefault(key => parameters[key]?.Length > ParameterMaxLength);
        if (tooLong is not null)
            return Result.Fail(new BusinessRuleError($"Parameter '{tooLong}' must be at most {ParameterMaxLength} characters.", nameof(request.Parameters)));

        return Result.Ok();
    }

    private async Task<Result> Send(
        EmployeeEventRequest request,
        EmployeeRef employee,
        string templateId,
        Dictionary<string, string> parameters,
        EmployeeRef? teacher = null)
    {
        Dictionary<RecipientRole, Recipient>? teacherRole = teacher is null
            ? null
            : new() { [RecipientRole.EnglishTeacher] = new Recipient(FullName(teacher), teacher.Email) };
        EmailSender? from = teacher is null ? null : new(teacher.Email, FullName(teacher));

        Result sent = await emailService.SendForEmployeeAsync(
            templateId, employee.Id, parameters, teacherRole, from: from);
        if (sent.IsFailed)
        {
            // Keep the reason (e.g. a missing template) out of the response: a 404 here would read as "employee not found".
            logger.LogError("Employee event {EventType} for employee {EmployeeId} could not be emailed: {Reasons}",
                request.EventType, request.EmployeeId, string.Join("; ", sent.Errors.Select(e => e.Message)));
            return sent.HasError<ServiceUnavailableError>()
                ? sent
                : Result.Fail(new BusinessRuleError("The notification email could not be sent."));
        }

        return Result.Ok();
    }

    private static string FullName(EmployeeRef employee)
    {
        return $"{employee.FirstName} {employee.LastName}";
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static string Level(Dictionary<string, string>? parameters, string key)
    {
        string? level = parameters?.GetValueOrDefault(key);
        return string.IsNullOrWhiteSpace(level) ? "-" : Html(level);
    }

    // One item per line, as the caller sends a list through a string-to-string dictionary.
    private static string HtmlList(Dictionary<string, string>? parameters, string key)
    {
        IEnumerable<string> items = (parameters?.GetValueOrDefault(key) ?? "")
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(item => $"<li>{Html(item)}</li>");
        return $"<ul>{string.Concat(items)}</ul>";
    }
}
