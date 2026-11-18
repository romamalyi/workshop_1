using System.Collections.Concurrent;

using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.IntegrationTests.Infrastructure;

// Invented people only: LeoHR holds personal data, and real names must not end up in tests.
public sealed class FakeEmployeeApi : IEmployeeApiClient
{
    private readonly ConcurrentDictionary<string, ApiEmployee> employees = new();

    public ApiEmployee Add(
        string? position = null,
        string? profile = null,
        string? seniority = null,
        string? subLevel = null,
        string? englishLevel = null,
        string? hrManagerId = null,
        string? managerId = null,
        string? id = null)
    {
        string guid = Guid.NewGuid().ToString();
        id ??= guid;
        string suffix = guid[..8];
        ApiEmployee employee = new(
            Id: id,
            FullName: $"Test Person{suffix}",
            FirstName: "Test",
            LastName: $"Person{suffix}",
            Email: $"person{suffix}@example.test",
            Active: true,
            Employment: "FullTime",
            Profile: profile,
            Seniority: seniority,
            SenioritySubLevel: subLevel,
            Location: null,
            Position: position,
            Manager: null,
            ManagerId: managerId,
            HrManager: null,
            HrManagerId: hrManagerId,
            Mentor: null,
            EnglishLevel: englishLevel,
            Birthday: null,
            StartDate: null,
            CareerStart: null,
            KeInfo: null,
            Level: null,
            Projects: []);
        employees[id] = employee;
        return employee;
    }

    public void SetManager(string employeeId, string? managerId)
    {
        employees[employeeId] = employees[employeeId] with { ManagerId = managerId };
    }

    public void SetHrManager(string employeeId, string? hrManagerId)
    {
        employees[employeeId] = employees[employeeId] with { HrManagerId = hrManagerId };
    }

    public void Deactivate(string id)
    {
        employees[id] = employees[id] with { Active = false };
    }

    public void Remove(string id)
    {
        employees.TryRemove(id, out _);
    }

    public Task<IReadOnlyList<ApiEmployee>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ApiEmployee>>([.. employees.Values]);
    }

    public Task RefreshAllAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<ApiEmployee?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return Task.FromResult(employees.GetValueOrDefault(id));
    }

    public Task<IReadOnlyList<EmployeeHistorySnapshot>> GetHistoryAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<EmployeeHistorySnapshot>>([]);
    }
}
