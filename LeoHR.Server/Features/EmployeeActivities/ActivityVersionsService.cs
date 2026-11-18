using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.EmployeeActivities;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.EmployeeActivities;

public class ActivityVersionsService(LeoHrDbContext db) : IActivityVersions
{
    public async Task MoveOpenRecordsAsync(int fromActivityId, int toActivityId)
    {
        List<EmployeeActivityEntity> openRecords = await db.EmployeeActivities
            .Where(x => x.ActivityId == fromActivityId
                && (x.Status == EmployeeActivityStatus.Planned || x.Status == EmployeeActivityStatus.SelfReported)
                && (x.Promotion == null || x.Promotion.Status != PromotionStatus.Sent))
            .ToListAsync();

        foreach (EmployeeActivityEntity record in openRecords)
            record.ActivityId = toActivityId;

        await db.SaveChangesAsync();
    }
}
