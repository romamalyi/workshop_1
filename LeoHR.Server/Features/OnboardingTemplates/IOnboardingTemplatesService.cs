using FluentResults;

using LeoHR.Server.Features.OnboardingTemplates.Dto;
using LeoHR.Server.Shared.Onboarding;

namespace LeoHR.Server.Features.OnboardingTemplates;

public interface IOnboardingTemplatesService
{
    Task<List<OnboardingTemplateDto>> GetAll();

    Task<Result<OnboardingTemplateDto>> GetById(int id);

    Task<Result<OnboardingTemplateDto>> Create(SaveTemplateRequest request);

    Task<Result<OnboardingTemplateDto>> Update(int id, SaveTemplateRequest request);

    Task<Result<OnboardingTemplateDto>> SetStatus(int id, bool isActive);

    Task<Result> Delete(int id);

    Task<Result<List<AffectedTypeDto>>> GetArchiveImpact(int id);

    Task<List<MilestoneOptionDto>> GetMilestones();

    Task<List<ResponsibleRoleOptionDto>> GetPersonRestrictionOptions();
}
