using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Shared.PromotionCriteria;

public record RequiredActivityDto(int Id, string Name);

public record PromotionCriteriaDto(
    int Id,
    string Technology,
    string Seniority,
    string SubLevel,
    int? WorkExperienceFromMonths,
    int? WorkExperienceToMonths,
    string? RequiredExperience,
    string EnglishLevel,
    int? RequiredActivityId,
    string? RequiredActivityName,
    string KeRequired,
    int? ActivitiesScore,
    bool IsActive,
    List<RequiredActivityDto> RequiredActivities
)
{
    public static PromotionCriteriaDto From(PromotionCriteriaEntity x)
    {
        return new(x.Id,
            x.Technology.ToString(),
            x.Seniority.ToString(),
            x.SubLevel.ToString(),
            x.WorkExperienceFromMonths,
            x.WorkExperienceToMonths,
            x.RequiredExperience,
            x.EnglishLevel.ToString(),
            x.RequiredActivityId,
            x.RequiredActivity?.Name,
            x.KeRequired.ToString(),
            x.ActivitiesScore,
            x.IsActive,
            x.RequiredActivity is { } required ? [new RequiredActivityDto(required.Id, required.Name)] : []
        );
    }
}
