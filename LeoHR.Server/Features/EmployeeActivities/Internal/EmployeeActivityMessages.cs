namespace LeoHR.Server.Features.EmployeeActivities.Internal;

public static class EmployeeActivityMessages
{
    public const string Promotion = "Promotion";
    public const string Activity = "Activity";
    public const string EmployeeActivity = "Employee activity";
    public const string Employee = "Employee";
    public const string ExternalIntegration = "External integration";

    public const string OnlyAdminsCreateApproved = "Only admins can create approved activities.";
    public const string AccomplishedOnRequired = "AccomplishedOn is required for SelfReported and Approved status.";
    public const string FutureAccomplishedDate = "Date accomplished cannot be in the future.";
    public const string FuturePlannedDate = "Planned date cannot be in the future.";
    public const string PlannedActivity = "Planned activity";
    public const string SentPromotionReadOnly = "The activities of a sent promotion can't be changed.";
    public const string ApprovedNotEditable = "An approved activity cannot be edited by non-admin.";
    public const string AlreadySelfReportedOrApproved = "Activity is already SelfReported or Approved.";
    public const string AlreadyApproved = "Activity is already Approved.";
    public const string PlannedNotApprovable = "A planned activity must be marked as done before it can be approved.";
    public const string CannotDeleteOthersActivity = "You cannot delete an activity created by another employee.";
    public const string OverallActiveSubmittedOnce =
        "This activity is valid for overall active period and can only be submitted once.";
    public const string AlreadyCompletedInLevel = "This activity has already been completed in your current level.";
    public const string IntegrationNotIdentified = "The calling integration could not be identified.";
}
