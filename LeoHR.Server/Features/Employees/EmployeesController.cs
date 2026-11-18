using FluentResults;

using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;
using LeoHR.Server.Shared.Models.Filter;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.Employees;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = nameof(Policies.ManageRead))]
public class EmployeesController(IEmployeesService employeesService) : ControllerBase
{
    [HttpGet]
    public Task<List<EmployeeDto>> GetActive()
    {
        return employeesService.GetActive();
    }

    // GetActive returns every active employee, unfiltered; Search applies filtering and pagination
    // server-side — the client infinite-scrolls through pages instead of loading everything at once.
    [HttpGet("search")]
    public Task<PagedResult<EmployeeDto>> Search([FromQuery] EmployeeFilterModel filter)
    {
        return employeesService.Search(filter);
    }

    [HttpGet("filter-options")]
    public Task<EmployeeFilterOptionsDto> GetFilterOptions()
    {
        return employeesService.GetFilterOptions();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        EmployeeDto? emp = await employeesService.GetById(id);
        return emp is null ? NotFound() : Ok(emp);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<List<EmployeeHistoryEventDto>>> GetHistory(string id, [FromQuery] string? userId = null)
    {
        return await employeesService.GetHistory(id, userId);
    }

    [HttpGet("projects")]
    public Task<List<EmployeeProjectsDto>> GetProjects([FromQuery] IList<string> userIds)
    {
        return employeesService.GetProjectsForUsers(userIds);
    }

    [HttpGet("seniorities")]
    public async Task<ActionResult<List<string>>> GetSeniorities()
    {
        Result<List<string>> result = await employeesService.GetLevels();
        return result.ToActionResult();
    }

    [HttpPut("{id}/pdp-url")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult> UpdatePdpUrl(string id, [FromBody] UpdatePdpUrlRequest request)
    {
        Result result = await employeesService.UpdatePdpUrl(id, request.PdpUrl);
        return result.ToActionResult();
    }
}
