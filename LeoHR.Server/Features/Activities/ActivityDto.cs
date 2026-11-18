using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Activities;

public record ActivityDto(
    int Id,
    string Category,
    string Name,
    string WithinPeriod,
    int Points,
    bool IsActive,
    string? Technology,
    bool IsPromotionRequirement
);

public record CategorizedActivity(
    string Category,
    List<ActivityDto> Activities
);
public record CreateActivityRequest(
    ActivityCategory Category,
    [Required] string Name,
    ActivityPeriod WithinPeriod,
    [Range(0, 1000)] int Points,
    Technology? Technology
);

public record UpdateActivityRequest(
    ActivityCategory Category,
    [Required] string Name,
    ActivityPeriod WithinPeriod,
    [Range(0, 1000)] int Points,
    Technology? Technology
);
