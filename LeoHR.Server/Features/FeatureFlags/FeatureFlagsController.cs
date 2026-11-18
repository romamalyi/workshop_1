using LeoHR.Server.Shared.FeatureFlags;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace LeoHR.Server.Features.FeatureFlags;

public record FeatureFlagsDto(bool KnowledgeEvaluation);

[ApiController]
[Route("api/features")]
public class FeatureFlagsController(IFeatureManager features) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<FeatureFlagsDto> Get()
    {
        return new FeatureFlagsDto(await features.IsEnabledAsync(FeatureFlagNames.KnowledgeEvaluation));
    }
}
