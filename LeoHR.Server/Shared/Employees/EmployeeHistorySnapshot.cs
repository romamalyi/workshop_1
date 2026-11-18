namespace LeoHR.Server.Shared.Employees;

/// <summary>
/// One point-in-time snapshot of an employee's tracked attributes, as returned by the Employee API's
/// per-user history endpoint. <see cref="Features.Employees.EmployeeHistoryAnalyzer"/> diffs a
/// timestamp-ordered series of these to build the History timeline.
/// </summary>
public sealed record EmployeeHistorySnapshot
{
    public DateTime Timestamp { get; init; }
    public bool Active { get; init; }
    public string? Employment { get; init; }
    public string? Location { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public string? Seniority { get; init; }
    public string? SenioritySubLevel { get; init; }
    public string? Profile { get; init; }
    public string? Position { get; init; }
    public string? Manager { get; init; }
    public string? HrManager { get; init; }
    public string? Mentor { get; init; }
    public string? EnglishLevel { get; init; }
    public int? Level { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? FullTimeStartDate { get; init; }
}
