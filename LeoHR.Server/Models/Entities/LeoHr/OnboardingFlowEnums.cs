namespace LeoHR.Server.Models.Entities.LeoHr;

public enum EmploymentType
{
    FullTime,
    PartTime,
}

public enum OnboardingTaskActivityKind
{
    AssigneeChanged,
    Started,
    StartUndone,
    FieldChanged,
    ChecklistItemChecked,
    ChecklistItemUnchecked,
    Completed,
    Reopened,
}
