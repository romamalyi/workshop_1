using FluentResults;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Attributes;
using LeoHR.Server.Shared.Extensions;
using LeoHR.Server.Shared.Extensions.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeoHR.Server.Features.EmployeeActivities;

[ApiController]
[Route("api/employees/{employeeId}/activities")]
[Authorize]
public class EmployeeActivitiesController(IEmployeeActivitiesService service) : ControllerBase
{
    [HttpGet]
    [OwnerOrManager(ReadOnly = true)]
    public async Task<ActionResult<List<EmployeeActivityDto>>> GetAll(string employeeId, [FromQuery] EmployeeActivityStatus? status)
    {
        return Ok(await service.GetAll(employeeId, status));
    }


    [HttpGet("{id}")]
    [OwnerOrManager(ReadOnly = true)]
    public async Task<ActionResult<EmployeeActivityDto>> GetById(string employeeId, int id)
    {
        EmployeeActivityDto? dto = await service.GetById(employeeId, id);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [OwnerOrManager]
    public async Task<ActionResult<EmployeeActivityDto>> Create(string employeeId, [FromBody] CreateEmployeeActivityRequest request)
    {
        Result<EmployeeActivityDto> result = await service.Create(employeeId, request, User);
        if (result.IsFailed) return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { employeeId, id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    [OwnerOrManager]
    public async Task<ActionResult<EmployeeActivityDto>> Update(string employeeId, int id, [FromBody] UpdateEmployeeActivityRequest request)
    {
        Result<EmployeeActivityDto> result = await service.Update(employeeId, id, request, User);
        return result.ToActionResult();
    }

    [HttpPut("{id}/self-report")]
    [OwnerOrManager]
    public async Task<ActionResult<EmployeeActivityDto>> SelfReport(string employeeId, int id, [FromBody] SelfReportRequest request)
    {
        Result<EmployeeActivityDto> result = await service.SelfReport(employeeId, id, request);
        return result.ToActionResult();
    }

    [HttpPut("{id}/approve")]
    [Authorize(Policy = nameof(Policies.Manage))]
    public async Task<ActionResult<EmployeeActivityDto>> Approve(string employeeId, int id, [FromBody] ApproveEmployeeActivityRequest request)
    {
        Result<EmployeeActivityDto> result = await service.Approve(
            employeeId, id, request.AccomplishedOn!.Value, request.ApprovedById, User);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [OwnerOrManager]
    public async Task<ActionResult> Delete(string employeeId, int id)
    {
        Result deleted = await service.Delete(employeeId, id, User);
        return deleted.ToActionResult();
    }

    // "~" drops the employee prefix: the list spans all employees.
    [HttpGet("~/api/planned-activities")]
    [Authorize(Policy = nameof(Policies.ManageRead))]
    public Task<List<PlannedActivityDto>> GetPlanned([FromQuery] PlannedActivityFilter filter)
    {
        return service.GetPlanned(filter);
    }

    [HttpPatch("{id}/planned-date")]
    [Authorize(Policy = nameof(Policies.ManageAdmin))]
    public async Task<ActionResult> UpdatePlannedDate(string employeeId, int id, [FromBody] UpdatePlannedDateRequest request)
    {
        Result result = await service.UpdatePlannedDate(employeeId, id, request.PlannedAt!.Value);
        return result.ToActionResult();
    }
}
