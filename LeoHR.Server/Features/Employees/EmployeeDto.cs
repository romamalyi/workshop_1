using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Features.Employees;

public record EmployeeDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? Profile,
    string? Seniority,
    string? SenioritySubLevel,
    string? Location,
    string Employment,
    string? EnglishLevel,
    DateOnly? Birthday,
    DateOnly? StartDate,
    DateOnly? CareerStart,
    string? Manager,
    string? HrManager,
    DateTime? LevelStartDate,
    List<string> Projects,
    string? PdpUrl);

public record EmployeeFilterOptionsDto(List<string> Seniorities, List<string> Profiles, List<string> Projects, List<string> HrManagers);

public record UpdateExtensionRequest(DateTime? LevelStartDate);
public record UpdatePdpUrlRequest(
    [Url]
    [MaxLength(2048)]
    string? PdpUrl);

public record EmployeeHistoryEventDto(
    string Description,
    string Category,
    DateTime Date,
    string? Detail = null,
    string? HrManager = null);

public record EmployeeProjectsDto(string UserId, List<string> Projects);

/// <summary>A completed LeoHR promotion as consumed by the history timeline.</summary>
/// <param name="CompletedOn">When it completed — <c>ReferenceDate</c>, or <c>CreatedAt</c> for legacy rows with no reference date.</param>
/// <param name="HrManager">The employee's HR manager when it was sent; null if that wasn't stored.</param>
public sealed record CompletedPromotion(DateTime CompletedOn, string FromRole, string ToRole, string? HrManager);
