using LeoHR.Server.Models.Entities.Base;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class PromotionEntity : BaseAuditableEntity
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string PromotionHolderId { get; set; } = string.Empty;
    public string FromRole { get; set; } = string.Empty;
    public string ToRole { get; set; } = string.Empty;
    public Technology Technology { get; set; } = Technology.Dotnet;
    public DateTime? ReferenceDate { get; set; }   // sent-at timestamp
    public DateTime? EmailSentAt { get; set; }        // set once the promotion email went out, so a retry of a failed send doesn't email again
    public DateTime? ReadyEmailSentAt { get; set; }   // set when HR was told the promotion is Ready, so a return to Ready doesn't email again
    public string? HrManager { get; set; }            // the employee's HR manager when the promotion was sent; null for promotions sent before this was stored
    public DateTime? LevelStartDate { get; set; }     // snapshot of the employee's level start when this promotion was created; frozen window boundary for "Overall in level" activities

    public PromotionStatus Status { get; set; } = PromotionStatus.InProgress;
    public int? PromotionCriteriaId { get; set; }     // set when a Soft criteria edit replaces the version this promotion read; null = the level's active criteria
    public List<string> ApprovedCriteriaLabels { get; set; } = [];
    public DateTime? CriteriaVerifiedAt { get; set; }
    public string? ExceptionNote { get; set; }        // read-only: HR's note on a promotion sent with unmet criteria
    public List<CriterionOverride> CriteriaOverrides { get; set; } = [];   // criteria HR marked met by hand, with a comment (empty if none) the employee sees
    public bool IsDeleted { get; set; }              // set when the employee leaves; hidden by a query filter
}
