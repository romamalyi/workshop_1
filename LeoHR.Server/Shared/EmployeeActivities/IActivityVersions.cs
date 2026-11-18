namespace LeoHR.Server.Shared.EmployeeActivities;

/// Implemented by Features/EmployeeActivities/ActivityVersionsService.
public interface IActivityVersions
{
    /// Moves Planned and SelfReported records outside Sent promotions to the activity's new version, and saves them.
    Task MoveOpenRecordsAsync(int fromActivityId, int toActivityId);
}
