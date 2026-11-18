using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Shared.Models.Filter;

public record PromotionFilterModel(
    PromotionStatus? Status = null,
    bool HasPendingActivities = false,
    List<string>? HrManagers = null,
    string? EmployeeId = null
) : BaseFilterModel;
