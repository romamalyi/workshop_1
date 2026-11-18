using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.EmployeeActivities;

public record EmployeeActivityDto(
    int Id,
    string EmployeeId,
    int ActivityId,
    EmployeeActivityStatus Status,
    DateOnly? AccomplishedOn,
    string? Note,
    int? PromotionId,
    int? PointsSnapshot,
    // TODO: CreatedById and ApprovedById will be derived from auth claims, not stored as-is
    string? CreatedById,
    string? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    ActivityPromotionInfo? PromotionInfo = null,
    string? CreatedByName = null
);
public record struct ActivityPromotionInfo(string FromRole, string ToRole);
public record CreateEmployeeActivityRequest(
    [Required] int ActivityId,
    [Required] EmployeeActivityStatus Status,
    DateOnly? AccomplishedOn,
    [MaxLength(500)] string? Note,
    int? PromotionId,
    // TODO: derive from authenticated user claims once auth is wired up
    string? CreatedById
);

public record UpdateEmployeeActivityRequest(
    int? ActivityId,
    DateOnly? AccomplishedOn,
    [MaxLength(500)] string? Note
);

public record ApproveEmployeeActivityRequest(
    string? ApprovedById,
    [Required] DateOnly? AccomplishedOn
);

public record SelfReportRequest(
    [Required] DateOnly AccomplishedOn,
    [MaxLength(500)] string? Note
);

public record PlannedActivityFilter(
    string? SearchTerm = null,
    List<string>? HrManagers = null,
    // A list, not an array: EF Core 9 cannot translate C# 14's span-based array.Contains.
    List<ActivityCategory>? Categories = null
);

public record PlannedActivityDto(
    int EmployeeActivityId,
    string EmployeeId,
    int? PromotionId,
    string EmployeeName,
    string Category,
    string Name,
    DateTime PlannedAt,
    string? HrManager
);

public record UpdatePlannedDateRequest(
    [Required] DateOnly? PlannedAt
);

public record IntegrationActivityDto(
    int Id,
    ActivityCategory Category,
    string Name,
    int Points,
    ActivityPeriod WithinPeriod
);

public record RecordEmployeeActivityRequest(
    [Required] string EmployeeId,
    [Range(1, int.MaxValue)] int ActivityId,
    [Required] DateOnly? AccomplishedOn,
    [MaxLength(500)] string? Note
);

public record IntegrationEmployeeActivitiesFilter(
    [Required] string EmployeeId,
    int? ActivityId = null,
    DateOnly? AccomplishedOn = null
);

public record IntegrationEmployeeActivityDto(
    int Id,
    int ActivityId,
    string ActivityName,
    DateOnly? AccomplishedOn,
    int? PointsSnapshot,
    string? Note,
    bool Editable
);

public record UpdateIntegrationEmployeeActivityRequest(
    [Range(1, int.MaxValue)] int ActivityId,
    [Required] DateOnly? AccomplishedOn,
    [MaxLength(500)] string? Note
);
