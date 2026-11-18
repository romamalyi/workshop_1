namespace LeoHR.Server.Models.Entities.LeoHr;

public enum OnboardingPhase
{
    BeforeFirstDay,
    FirstDay,
    FirstWeek,
    SecondWeek,
    FirstMonth,
    ThirdMonth,
}

public enum DueDirection
{
    Before,
    After,
}

public enum DueAnchor
{
    StartDate,
    EndOfProbation,
}

public enum TemplateAppliesTo
{
    AllHires,
    Optional,
}

public enum TemplateFieldType
{
    Note,
    Checklist,
    EmployeePicker,
}
