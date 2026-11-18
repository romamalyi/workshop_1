using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Features.AppSettings;

public record EmployeeHistorySettingsDto(
    bool ShowManager,
    bool ShowMentor,
    bool ShowEmployment
);

public record SaveEmployeeHistorySettingsRequest(
    bool ShowManager,
    bool ShowMentor,
    bool ShowEmployment
);

public record PromotionDefaultHolderDto(string EmployeeId);

public record SavePromotionDefaultHolderRequest(
    [Required, MaxLength(100)] string EmployeeId
);
