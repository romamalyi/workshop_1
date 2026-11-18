using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.PromotionCriteria;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionRules
{
    // Sent promotions are history, so the one automatic check never touches them.
    public static bool NeedsCriteriaVerification(PromotionEntity promotion)
    {
        return promotion.CriteriaVerifiedAt is null && promotion.Status != PromotionStatus.Sent;
    }

    public static IEnumerable<EmployeeActivityEntity> GetValidApprovedActivities(
        IEnumerable<EmployeeActivityEntity> activities,
        DateTime referenceDate,
        DateTime? levelStartDate
    )
    {
        return activities.Where(a => a.Status == EmployeeActivityStatus.Approved &&
                               PassesWindow(a.Activity.WithinPeriod, a.AccomplishedOn, referenceDate, levelStartDate));
    }

    public static bool PassesWindow(
        ActivityPeriod period,
        DateOnly? accomplishedOn,
        DateTime refDate,
        DateTime? levelStartDate)
    {
        if (accomplishedOn is null) return period == ActivityPeriod.OverallActive;

        var date = accomplishedOn.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return period switch
        {
            ActivityPeriod.SixMonths => date >= refDate.Date.AddMonths(-6),
            ActivityPeriod.TwelveMonths => date >= refDate.Date.AddMonths(-12),
            ActivityPeriod.OverallActive => true,
            ActivityPeriod.OverallInLevel => levelStartDate is null || date >= levelStartDate.Value.Date,
            _ => false
        };
    }

    public static int LifetimeRank(ActivityPeriod period)
    {
        return period switch
        {
            ActivityPeriod.OverallActive => 3,
            ActivityPeriod.TwelveMonths => 2,
            ActivityPeriod.SixMonths => 1,
            _ => 0
        };
    }

    public static bool IsRequiredActivity(EmployeeActivityEntity a, IReadOnlyCollection<int> requiredActivityIds)
    {
        return requiredActivityIds.Contains(a.ActivityId);
    }

    // Promoting across several levels takes every level's requirements: points add up, and a requirement repeated
    // across levels counts once at its highest value. `levels` run lowest to highest; the last is the target.
    public static PromotionCriteriaDto MergeCriteria(IReadOnlyList<PromotionCriteriaDto> levels)
    {
        return levels[^1] with
        {
            WorkExperienceFromMonths = levels.Max(l => l.WorkExperienceFromMonths),
            WorkExperienceToMonths = levels.Max(l => l.WorkExperienceToMonths),
            RequiredExperience = levels.Select(l => l.RequiredExperience).LastOrDefault(s => !string.IsNullOrWhiteSpace(s)),
            EnglishLevel = levels.Max(l => ParseOrLowest<EnglishLevel>(l.EnglishLevel)).ToString(),
            KeRequired = levels.Max(l => ParseOrLowest<KeRequirement>(l.KeRequired)).ToString(),
            ActivitiesScore = levels.Any(l => l.ActivitiesScore.HasValue) ? levels.Sum(l => l.ActivitiesScore ?? 0) : null,
            RequiredActivities = [.. levels.SelectMany(l => l.RequiredActivities).DistinctBy(a => a.Id)],
        };
    }

    // Keep in sync with buildCriteriaChecklist in promotion-criteria.util.ts: the labels are the tick keys.
    public static List<string> RequiredCriteriaLabels(PromotionCriteriaDto criteria)
    {
        List<string> labels = [];
        if (criteria.WorkExperienceFromMonths is not null || criteria.WorkExperienceToMonths is not null)
            labels.Add(CriteriaLabels.WorkExperience);
        if (HasValue(criteria.RequiredExperience)) labels.Add(CriteriaLabels.RequiredExperience);
        labels.Add(CriteriaLabels.EnglishLevel);
        if (HasValue(criteria.KeRequired)) labels.Add(CriteriaLabels.KeRequired);

        bool severalAchievements = criteria.RequiredActivities.Count > 1;
        labels.AddRange(criteria.RequiredActivities
            .Where(a => HasValue(a.Name))
            .Select(a => severalAchievements
                ? $"{CriteriaLabels.RequiredAchievement}: {a.Name}"
                : CriteriaLabels.RequiredAchievement));

        if (criteria.ActivitiesScore is not null) labels.Add(CriteriaLabels.ActivityScore);
        return labels;
    }

    public static HashSet<string> ResolvedLabels(PromotionEntity promotion)
    {
        return [.. promotion.ApprovedCriteriaLabels, .. promotion.CriteriaOverrides.Select(o => o.Label)];
    }

    public static bool AllCriteriaMet(PromotionCriteriaDto criteria, PromotionEntity promotion)
    {
        HashSet<string> resolved = ResolvedLabels(promotion);
        return RequiredCriteriaLabels(criteria).All(resolved.Contains);
    }

    public static void SyncStatus(PromotionEntity promotion, PromotionCriteriaDto criteria)
    {
        if (promotion.Status == PromotionStatus.Sent) return;
        promotion.Status = AllCriteriaMet(criteria, promotion) ? PromotionStatus.Ready : PromotionStatus.InProgress;
    }

    public static HashSet<string> EvaluateCriteria(
        IEnumerable<string> existingLabels,
        PromotionCriteriaDto criteria,
        int approvedPoints,
        EnglishLevel? englishLevel)
    {
        var labels = new HashSet<string>(existingLabels);
        // A tick HR set by hand must survive an automatic evaluation.
        if (MeetsEnglish(criteria, englishLevel)) labels.Add(CriteriaLabels.EnglishLevel);

        if (criteria.ActivitiesScore is null || approvedPoints >= criteria.ActivitiesScore)
            labels.Add(CriteriaLabels.ActivityScore);
        else
            labels.Remove(CriteriaLabels.ActivityScore);
        return labels;
    }

    // An unrecognised value counts as the weakest requirement, so it never outranks a real one.
    private static T ParseOrLowest<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse(value, out T parsed) ? parsed : default;
    }

    private static bool HasValue(string? value)
    {
        string trimmed = value?.Trim().ToLowerInvariant() ?? string.Empty;
        return trimmed is not ("" or "—" or "none" or "-");
    }

    private static bool MeetsEnglish(PromotionCriteriaDto criteria, EnglishLevel? englishLevel)
    {
        return englishLevel.HasValue
        && Enum.TryParse<EnglishLevel>(criteria.EnglishLevel, out EnglishLevel required)
        && (int)englishLevel.Value >= (int)required;
    }
}
