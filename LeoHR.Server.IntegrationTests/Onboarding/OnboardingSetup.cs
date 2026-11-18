using LeoHR.Server.Features.OnboardingTemplates.Dto;
using LeoHR.Server.Features.OnboardingTypes.Dto;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.IntegrationTests.Onboarding;

public static class OnboardingSetup
{
    public static async Task<int> CreateTemplate(HttpClient onboardingAdmin, string? roleKey)
    {
        int milestoneId = (await onboardingAdmin.GetOk<List<MilestoneOptionDto>>("api/onboarding/templates/milestones"))[0].Id;
        OnboardingTemplateDto template = await (await onboardingAdmin.PostJson("api/onboarding/templates", new SaveTemplateRequest(
            $"Test task {Guid.NewGuid():N}", OnboardingPhase.FirstDay, milestoneId, roleKey, Description: null,
            DueOffsetDays: 0, DueDirection.After, DueAnchor.StartDate, TemplateAppliesTo.AllHires,
            SuggestedFor: null, Fields: null))).ReadOk<OnboardingTemplateDto>();
        return template.Id;
    }

    public static SaveTypeRequest TypeRequest(string name, IEnumerable<int> templateIds)
    {
        return new SaveTypeRequest(name, Description: null, IsArchived: false,
            [.. templateIds.Select(id => new TypeTemplateSelectionDto(id, IsEnabled: true))]);
    }
}
