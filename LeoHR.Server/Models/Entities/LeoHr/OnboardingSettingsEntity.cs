using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

// Company-wide onboarding settings: the table holds a single row.
public class OnboardingSettingsEntity : BaseAuditableEntity
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public int DaysBeforeStartToNotify { get; set; } = 14;
    public bool WeeklyDigestEnabled { get; set; }
    public bool OverdueAlertsEnabled { get; set; }

    public int ProbationMonths { get; set; } = 3;
    public int MaxOnboardingHours { get; set; } = 15;
}
