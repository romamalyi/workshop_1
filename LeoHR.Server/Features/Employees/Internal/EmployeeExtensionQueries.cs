using LeoHR.Server.Models.Entities.LeoHr;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.Employees.Internal;

public static class EmployeeExtensionQueries
{
    public static async Task<EmployeeExtensionEntity> GetOrAddAsync(
        this DbSet<EmployeeExtensionEntity> extensions,
        string employeeId)
    {
        EmployeeExtensionEntity? extension = await extensions.FindAsync(employeeId);
        if (extension is not null) return extension;

        extension = new EmployeeExtensionEntity { EmployeeId = employeeId };
        extensions.Add(extension);
        return extension;
    }
}
