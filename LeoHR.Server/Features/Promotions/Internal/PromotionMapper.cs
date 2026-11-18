using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.PromotionCriteria;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class PromotionMapper
{
    public static string FullName(EmployeeRef? emp)
    {
        return $"{emp?.FirstName} {emp?.LastName}".Trim();
    }

    public static PromotionDto ToSummaryDto(PromotionEntity p, EmployeeRef? emp)
    {
        return new(Id: p.Id,
                EmployeeId: p.EmployeeId,
                FirstName: emp?.FirstName ?? string.Empty,
                LastName: emp?.LastName ?? string.Empty,
                FromRole: p.FromRole,
                ToRole: p.ToRole,
                Status: p.Status);
    }

    public static List<PromotionDto> ToSummaryDtos(IEnumerable<PromotionRow> rows, IEnumerable<EmployeeRef> employees)
    {
        var employeeMap = employees.ToDictionary(e => e.Id);
        return [.. rows.Select(r =>
        {
            employeeMap.TryGetValue(r.EmployeeId, out EmployeeRef? emp);
            return new PromotionDto(
                Id: r.Id,
                EmployeeId: r.EmployeeId,
                FirstName: emp?.FirstName ?? string.Empty,
                LastName: emp?.LastName ?? string.Empty,
                FromRole: r.FromRole,
                ToRole: r.ToRole,
                Status: r.Status,
                HasPendingActivities: r.HasPendingActivities,
                SentAt: r.SentAt);
        })];
    }

    public static PromotionActivityDto MapActivity(EmployeeActivityEntity a)
    {
        return new(
        EmployeeActivityId: a.Id,
        ActivityId: a.ActivityId,
        Category: a.Activity.Category.ToString(),
        Name: a.Activity.Name,
        WithinPeriod: a.Activity.WithinPeriod.ToString(),
        PointsSnapshot: a.PointsSnapshot ?? a.Activity.Points,
        Note: a.Note,
        AccomplishedOn: a.AccomplishedOn,
        AddedAt: a.CreatedAt,
        Status: a.Status,
        CreatedById: a.CreatedById,
        IsCarriedOver: a.CarriedFromPromotionId != null);
    }

    public static ActivityBuckets BuildActivityBuckets(
        List<EmployeeActivityEntity> activities,
        DateTime refDate,
        DateTime? levelStartDate)
    {
        var approved = PromotionRules.GetValidApprovedActivities(activities, refDate, levelStartDate)
            .Select(MapActivity)
            .ToList();
        var planned = activities
            .Where(a => a.Status == EmployeeActivityStatus.Planned)
            .Select(MapActivity)
            .ToList();
        var selfReported = activities
            .Where(a => a.Status == EmployeeActivityStatus.SelfReported
                     && PromotionRules.PassesWindow(a.Activity.WithinPeriod, a.AccomplishedOn, refDate, levelStartDate))
            .Select(MapActivity)
            .ToList();
        return new ActivityBuckets(approved, planned, selfReported);
    }

    public static PromotionDetailDto BuildDetailDto(
        PromotionEntity promotion,
        EmployeeRef? emp,
        PromotionCriteriaDto? criteria,
        ActivityBuckets buckets,
        List<PromotionHistoryItem> history,
        EmployeeRef? holder,
        List<string> targetLevels,
        KnowledgeEvaluationStatus? keStatus)
    {
        return new PromotionDetailDto(
            Id: promotion.Id,
            EmployeeId: promotion.EmployeeId,
            FirstName: emp?.FirstName ?? string.Empty,
            LastName: emp?.LastName ?? string.Empty,
            FromRole: promotion.FromRole,
            ToRole: promotion.ToRole,
            Status: promotion.Status,
            Technology: promotion.Technology.ToString(),
            CreatedAt: promotion.CreatedAt,
            Criteria: criteria,
            Activities: buckets.Approved,
            PlannedActivities: buckets.Planned,
            SelfReportedActivities: buckets.SelfReported,
            PointsAchieved: buckets.Approved.Sum(a => a.PointsSnapshot),
            PointsPlanned: buckets.Planned.Sum(a => a.PointsSnapshot),
            PointsRequired: criteria?.ActivitiesScore,
            PromotionHistory: history,
            ApprovedCriteriaLabels: promotion.ApprovedCriteriaLabels,
            EmployeeEnglishLevel: ResolveEnglishLevel(emp),
            PromotionHolderId: promotion.PromotionHolderId,
            PromotionHolderName: holder is null ? null : $"{holder.FirstName} {holder.LastName}".Trim(),
            NeedsCriteriaVerification: criteria is not null && PromotionRules.NeedsCriteriaVerification(promotion),
            HrManager: emp?.HrManager,
            TargetLevels: targetLevels,
            SentAt: promotion.Status == PromotionStatus.Sent ? promotion.ReferenceDate ?? promotion.CreatedAt : null,
            ExceptionNote: promotion.ExceptionNote,
            CriteriaOverrides: [.. promotion.CriteriaOverrides.Select(o => new CriterionOverrideDto(o.Label, o.Text))],
            RequiredCriteriaLabels: criteria is null ? [] : PromotionRules.RequiredCriteriaLabels(criteria),
            HrManagerId: emp?.HrManagerId,
            KeStatus: keStatus);
    }

    public static PromotionEntity BuildPromotion(
        CreatePromotionRequest request,
        DateTime? levelStartDate)
    {
        return new()
        {
            EmployeeId = request.EmployeeId,
            PromotionHolderId = request.PromotionHolderId,
            FromRole = request.FromRole,
            ToRole = request.ToRole,
            Technology = request.Technology,
            Status = PromotionStatus.InProgress,
            LevelStartDate = levelStartDate,
        };
    }

    public static EnglishLevel? ResolveEnglishLevelEnum(EmployeeRef? emp)
    {
        string? raw = emp?.EnglishLevel;
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (englishLevelMap.TryGetValue(raw.Trim(), out EnglishLevel exact)) return exact;

        string normalized = new([.. raw.ToLowerInvariant().Where(c => char.IsLetter(c) || c == ' ')]);
        string compact = normalized.Replace(" ", string.Empty);
        bool low = normalized.Contains("low");
        bool strong = normalized.Contains("strong");

        if (compact.Contains("proficien") || compact.Contains("advanced")) return EnglishLevel.Advanced;
        if (compact.Contains("upperintermediate")) return EnglishLevel.UpperIntermediate;
        if (compact.Contains("preintermediate") || compact.Contains("beginner") || compact.Contains("elementary"))
            return null;
        if (compact.Contains("intermediate"))
        {
            return strong ? EnglishLevel.IntermediateStrong
                 : low ? EnglishLevel.IntermediateLow
                 : EnglishLevel.Intermediate;
        }

        return null;
    }

    private static string? ResolveEnglishLevel(EmployeeRef? emp)
    {
        return ResolveEnglishLevelEnum(emp)?.ToString();
    }

    private static readonly Dictionary<string, EnglishLevel> englishLevelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IntermediateLow"] = EnglishLevel.IntermediateLow,
        ["Intermediate Low"] = EnglishLevel.IntermediateLow,
        ["IntLow"] = EnglishLevel.IntermediateLow,
        ["Intermediate"] = EnglishLevel.Intermediate,
        ["Int"] = EnglishLevel.Intermediate,
        ["IntermediateStrong"] = EnglishLevel.IntermediateStrong,
        ["Intermediate Strong"] = EnglishLevel.IntermediateStrong,
        ["IntStrong"] = EnglishLevel.IntermediateStrong,
        ["UpperIntermediate"] = EnglishLevel.UpperIntermediate,
        ["Upper Intermediate"] = EnglishLevel.UpperIntermediate,
        ["UpperInt"] = EnglishLevel.UpperIntermediate,
        ["Advanced"] = EnglishLevel.Advanced,
    };
}
