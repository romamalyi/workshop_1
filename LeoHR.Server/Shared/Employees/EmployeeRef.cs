namespace LeoHR.Server.Shared.Employees;

public record EmployeeRef(string Id, string FirstName, string LastName, string Email, string? EnglishLevel, DateOnly? CareerStart, string? KEInfo, string? Manager, string? HrManager, string? Profile, string? Seniority, string? SenioritySubLevel, string? ManagerId, string? HrManagerId);
