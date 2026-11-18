using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.PromotionCriteria;

namespace LeoHR.Server.Features.Promotions;

public record PromotionDto(
    int Id,
    string EmployeeId,
    string FirstName,
    string LastName,
    string FromRole,
    string ToRole,
    PromotionStatus Status,
    bool HasPendingActivities = false,
    DateTime? SentAt = null
);

public record CreatePromotionRequest(
    [Required, MaxLength(100)] string EmployeeId,
    string FromRole,
    [Required, MaxLength(200)] string ToRole,
    [EnumDataType(typeof(Technology))] Technology Technology,
    [MaxLength(100)] string PromotionHolderId
);

public record PromotionTargetLevelsDto(string? FromRole, Technology? Technology, IReadOnlyList<string> Levels);

public record EmployeeLevelDto(string? Seniority, string? Technology);

public record PromotionActivityDto(
    int EmployeeActivityId,
    int ActivityId,
    string Category,
    string Name,
    string WithinPeriod,
    int PointsSnapshot,
    string? Note,
    DateOnly? AccomplishedOn,
    DateTime AddedAt,
    EmployeeActivityStatus Status,
    string? CreatedById,
    bool IsCarriedOver = false
);
public static class CriteriaLabels
{
    public const string EnglishLevel = "English level";
    public const string ActivityScore = "Activity score required";
    public const string WorkExperience = "Work experience";
    public const string RequiredExperience = "Required experience";
    public const string KeRequired = "KE required";
    public const string RequiredAchievement = "Required achievement";
}
public record AddActivitiesResult(
    List<PromotionActivityDto> Added,
    List<int> NotFound,
    List<int> AlreadyExists
);

public record PromotionDetailDto(
    int Id,
    string EmployeeId,
    string FirstName,
    string LastName,
    string FromRole,
    string ToRole,
    PromotionStatus Status,
    string Technology,
    DateTime CreatedAt,
    PromotionCriteriaDto? Criteria,
    IReadOnlyList<PromotionActivityDto> Activities,
    IReadOnlyList<PromotionActivityDto> PlannedActivities,
    IReadOnlyList<PromotionActivityDto> SelfReportedActivities,
    int PointsAchieved,
    int PointsPlanned,
    int? PointsRequired,
    string PromotionHolderId,
    IReadOnlyList<PromotionHistoryItem>? PromotionHistory = null,
    IReadOnlyList<string>? ApprovedCriteriaLabels = null,
    string? EmployeeEnglishLevel = null,
    string? PromotionHolderName = null,
    bool NeedsCriteriaVerification = false,
    string? HrManager = null,
    IReadOnlyList<string>? TargetLevels = null,
    DateTime? SentAt = null,
    string? ExceptionNote = null,
    IReadOnlyList<CriterionOverrideDto>? CriteriaOverrides = null,
    IReadOnlyList<string>? RequiredCriteriaLabels = null,
    string? HrManagerId = null,
    KnowledgeEvaluationStatus? KeStatus = null
);

public record CriterionOverrideDto(string Label, string Text);

public record SetCriterionRequest(
    [Required, MaxLength(200)] string Label,
    bool Met,
    [MaxLength(500)] string? Comment
);

public record UpdatePromotionRequest(
    [Required, MaxLength(100)] string PromotionHolderId,
    [Required, MaxLength(200)] string ToRole
);

public record PendingApprovalDto(
    int EmployeeActivityId,
    string EmployeeId,
    int? PromotionId,
    string EmployeeName,
    string? FromRole,
    string? ToRole,
    string Category,
    string Name,
    string WithinPeriod,
    int PointsSnapshot,
    EmployeeActivityStatus Status,
    DateOnly? AccomplishedOn,
    string? Note
);

public record PromotionHistoryItem(
    int Id,
    string FromRole,
    string ToRole,
    PromotionStatus Status,
    DateTime CreatedAt
);

public record AddActivityRequest(int ActivityId, string? Note);

public record SendPromotionRequest([MaxLength(20)] List<string>? ExtraCcEmployeeIds = null);

public record ApproveActivityRequest(string? ApprovedById, [Required] DateOnly? AccomplishedOn);

public record PromotionRow(
    int Id,
    string EmployeeId,
    string FromRole,
    string ToRole,
    PromotionStatus Status,
    bool HasPendingActivities,
    DateTime? SentAt
);

public record ActivityBuckets(
    List<PromotionActivityDto> Approved,
    List<PromotionActivityDto> Planned,
    List<PromotionActivityDto> SelfReported
);
