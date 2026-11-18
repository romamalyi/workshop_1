using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Onboarding;

using Microsoft.EntityFrameworkCore;

namespace LeoHR.Server.Features.OnboardingFlows;

public class OnboardingFlowWarningsService(
    LeoHrDbContext db,
    IEmployeeLookup employeeLookup,
    IEmployeeApiClient employeeApi,
    ILogger<OnboardingFlowWarningsService> logger) : IOnboardingFlowWarnings
{
    public async Task<int> CountFlowsWithWarningsAsync()
    {
        List<OnboardingFlowEntity> flows = await db.OnboardingFlows.AsNoTracking().Include(f => f.Tasks).ToListAsync();
        FlowWarningContext context = await new OnboardingFlowData(db, employeeLookup, employeeApi, logger).LoadWarningContext();
        return flows.Count(f => FlowWarnings.Build(f, OnboardingFlowMapper.IsCompleted(f), context).Count > 0);
    }
}
