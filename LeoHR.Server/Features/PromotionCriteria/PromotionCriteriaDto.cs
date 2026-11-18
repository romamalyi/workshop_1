using System.ComponentModel.DataAnnotations;

using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.PromotionCriteria;

public record PromotionCriteriaRequest(
    Technology Technology,
    Seniority Seniority,
    SubLevel SubLevel,
    [Range(0, 600)] int? WorkExperienceFromMonths,
    [Range(0, 600)] int? WorkExperienceToMonths,
    [MaxLength(300)] string? RequiredExperience,
    EnglishLevel EnglishLevel,
    int? RequiredActivityId,
    KeRequirement KeRequired,
    int? ActivitiesScore
);
