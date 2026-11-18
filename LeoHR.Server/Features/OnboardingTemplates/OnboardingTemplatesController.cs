using FluentResults;

using LeoHR.Server.Features.OnboardingTemplates.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.OnboardingTemplates;

[ApiController]
[Route("api/onboarding/templates")]
[Authorize(Policy = nameof(Policies.OnboardingAdmin))]
public class OnboardingTemplatesController(IOnboardingTemplatesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OnboardingTemplateDto>>> GetAll()
    {
        List<OnboardingTemplateDto> templates = await service.GetAll();
        return Ok(templates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OnboardingTemplateDto>> GetById(int id)
    {
        Result<OnboardingTemplateDto> result = await service.GetById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<OnboardingTemplateDto>> Create([FromBody] SaveTemplateRequest request)
    {
        Result<OnboardingTemplateDto> result = await service.Create(request);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OnboardingTemplateDto>> Update(int id, [FromBody] SaveTemplateRequest request)
    {
        Result<OnboardingTemplateDto> result = await service.Update(id, request);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OnboardingTemplateDto>> SetStatus(int id, [FromBody] SetTemplateStatusRequest request)
    {
        Result<OnboardingTemplateDto> result = await service.SetStatus(id, request.IsActive);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/archive-impact")]
    public async Task<ActionResult<List<AffectedTypeDto>>> GetArchiveImpact(int id)
    {
        Result<List<AffectedTypeDto>> result = await service.GetArchiveImpact(id);
        return result.ToActionResult();
    }

    [HttpGet("milestones")]
    public async Task<ActionResult<List<MilestoneOptionDto>>> GetMilestones()
    {
        return Ok(await service.GetMilestones());
    }

    [HttpGet("person-restrictions")]
    public async Task<ActionResult<List<ResponsibleRoleOptionDto>>> GetPersonRestrictionOptions()
    {
        return Ok(await service.GetPersonRestrictionOptions());
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        Result result = await service.Delete(id);
        return result.ToActionResult();
    }
}
