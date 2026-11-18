using FluentResults;

using LeoHR.Server.Features.OnboardingTypes.Dto;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.OnboardingTypes;

[ApiController]
[Route("api/onboarding/types")]
[Authorize(Policy = nameof(Policies.OnboardingWrite))]
public class OnboardingTypesController(IOnboardingTypesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OnboardingTypesDto>> GetAll()
    {
        OnboardingTypesDto types = await service.GetAll();
        return Ok(types);
    }

    [HttpGet("library")]
    public async Task<ActionResult<List<TypeLibraryTemplateDto>>> GetLibrary()
    {
        List<TypeLibraryTemplateDto> templates = await service.GetLibrary();
        return Ok(templates);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OnboardingTypeDetailDto>> GetById(int id)
    {
        Result<OnboardingTypeDetailDto> result = await service.GetById(id);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<OnboardingTypeDto>> Create([FromBody] SaveTypeRequest request)
    {
        Result<OnboardingTypeDto> result = await service.Create(request);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<OnboardingTypeDto>> Update(int id, [FromBody] SaveTypeRequest request)
    {
        Result<OnboardingTypeDto> result = await service.Update(id, request);
        return result.ToActionResult();
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<OnboardingTypeDto>> Duplicate(int id)
    {
        Result<OnboardingTypeDto> result = await service.Duplicate(id);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/archived")]
    public async Task<ActionResult<OnboardingTypeDto>> SetArchived(int id, [FromBody] SetTypeArchivedRequest request)
    {
        Result<OnboardingTypeDto> result = await service.SetArchived(id, request.IsArchived);
        return result.ToActionResult();
    }
}
