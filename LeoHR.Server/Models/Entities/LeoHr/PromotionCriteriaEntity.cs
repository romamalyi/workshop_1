using System.ComponentModel.DataAnnotations;

namespace LeoHR.Server.Models.Entities.LeoHr;

public class PromotionCriteriaEntity
{
    public int Id { get; set; }
    public Technology Technology { get; set; }
    public Seniority Seniority { get; set; }
    public SubLevel SubLevel { get; set; }

    public int? WorkExperienceFromMonths { get; set; }
    public int? WorkExperienceToMonths { get; set; }

    [MaxLength(300)]
    public string? RequiredExperience { get; set; }

    public EnglishLevel EnglishLevel { get; set; }

    public int? RequiredActivityId { get; set; }
    public ActivityEntity? RequiredActivity { get; set; }

    public KeRequirement KeRequired { get; set; }
    public int? ActivitiesScore { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
