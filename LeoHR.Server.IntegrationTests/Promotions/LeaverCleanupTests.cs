using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Models.Filter;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class LeaverCleanupTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task An_employee_marked_inactive_loses_the_open_promotion_and_its_activities()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);
        int employeeActivityId = await scenario.AddReportedActivity(points: 10);
        factory.Employees.Deactivate(scenario.Employee.Id);

        await RunCleanup();

        PromotionEntity promotion = await LoadPromotion(scenario.PromotionId);
        Assert.True(promotion.IsDeleted);
        Assert.False(await ActivityExists(employeeActivityId));
        PagedResult<PromotionDto> list = await scenario.Hr.GetOk<PagedResult<PromotionDto>>(
            $"api/promotions?employeeId={scenario.Employee.Id}");
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task An_active_employee_keeps_the_open_promotion_and_every_activity()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);
        int approvedId = await scenario.AddReportedActivity(points: 10);
        await (await scenario.Approve(scenario.Hr, approvedId)).EnsureSuccess();
        int plannedId = await scenario.AddPlannedActivity(points: 10);

        await RunCleanup();

        Assert.False((await LoadPromotion(scenario.PromotionId)).IsDeleted);
        Assert.True(await ActivityExists(approvedId));
        Assert.True(await ActivityExists(plannedId));
    }

    [Fact]
    public async Task An_employee_missing_from_the_employee_list_keeps_the_open_promotion_and_every_activity()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);
        int approvedId = await scenario.AddReportedActivity(points: 10);
        await (await scenario.Approve(scenario.Hr, approvedId)).EnsureSuccess();
        factory.Employees.Remove(scenario.Employee.Id);

        await RunCleanup();

        Assert.False((await LoadPromotion(scenario.PromotionId)).IsDeleted);
        Assert.True(await ActivityExists(approvedId));
    }

    [Fact]
    public async Task A_run_over_the_leaver_limit_removes_nothing()
    {
        List<PromotionScenario> scenarios = [];
        for (int i = 0; i <= new PromotionsOptions().MaxLeaversPerCleanup; i++)
        {
            scenarios.Add(await PromotionScenario.Create(factory, requiredScore: 60));
        }

        try
        {
            scenarios.ForEach(s => factory.Employees.Deactivate(s.Employee.Id));

            int removed = await RunCleanup();

            Assert.Equal(0, removed);
            foreach (PromotionScenario scenario in scenarios)
            {
                Assert.False((await LoadPromotion(scenario.PromotionId)).IsDeleted);
            }
        }
        finally
        {
            // Left inactive, these would trip the limit in every later cleanup run of the shared database.
            scenarios.ForEach(s => factory.Employees.Remove(s.Employee.Id));
        }
    }

    private async Task<int> RunCleanup()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPromotionsService>().DeleteLeaversPromotions();
    }

    private async Task<PromotionEntity> LoadPromotion(int promotionId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        return await db.Promotions.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == promotionId);
    }

    private async Task<bool> ActivityExists(int employeeActivityId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        return await db.EmployeeActivities.IgnoreQueryFilters().AnyAsync(a => a.Id == employeeActivityId);
    }
}
