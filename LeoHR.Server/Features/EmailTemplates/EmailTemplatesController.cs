
using FluentResults;

using LeoHR.Server.Shared.Email;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LeoHR.Server.Features.EmailTemplates;

[ApiController]
[Route("api/email-templates")]
[Authorize(Policy = nameof(Policies.Manage))]
public class EmailTemplatesController(IEmailTemplatesService emailTemplatesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<EmailTemplateSettingsResponse>>> GetByCategory([FromQuery, BindRequired] EmailTemplateCategory category)
    {
        return await emailTemplatesService.GetByCategory(category);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EmailTemplateResponse>> GetById(string id)
    {
        EmailTemplateResponse? template = await emailTemplatesService.GetById(id);
        if (template is null) return NotFound();
        return template;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(string id, [FromBody] UpdateEmailTemplateRequest request)
    {
        Result result = await emailTemplatesService.Update(id, request);
        return result.ToActionResult();
    }
}
