using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public enum EmployeeActivityStatus
{
    Planned,
    SelfReported,
    Approved,
}

public class EmployeeActivityEntity : BaseAuditableEntity
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public int ActivityId { get; set; }
    public ActivityEntity Activity { get; set; } = null!;
    public EmployeeActivityStatus Status { get; set; }
    public DateOnly? AccomplishedOn { get; set; }
    public string? Note { get; set; }
    public int? PromotionId { get; set; }
    public PromotionEntity? Promotion { get; set; }
    public int? PointsSnapshot { get; set; }
    public string? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? CarriedFromPromotionId { get; set; }
    // Admin's correction of when it was really planned; CreatedAt stays the audit value.
    public DateTime? PlannedAt { get; set; }
}
