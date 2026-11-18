using LeoHR.Server.Features.Employees.Internal;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.Features.Employees;

public class EmployeeLevelStartService(LeoHrDbContext leoHrDb) : IEmployeeLevelStart
{
    public async Task SetLevelStartDateAsync(string employeeId, DateTime date)
    {
        EmployeeExtensionEntity extension = await leoHrDb.EmployeeExtensions.GetOrAddAsync(employeeId);
        extension.LevelStartDate = date;
        await leoHrDb.SaveChangesAsync();
    }
}
