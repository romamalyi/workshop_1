using System.Net;

using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionActivityApprovalTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task HR_approving_a_reported_activity_counts_its_points_toward_the_promotion()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 100);
        int employeeActivityId = await scenario.AddReportedActivity(points: 15);

        HttpResponseMessage response = await scenario.Approve(scenario.Hr, employeeActivityId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        PromotionDetailDto detail = await scenario.GetDetail();
        PromotionActivityDto approved = Assert.Single(detail.Activities);
        Assert.Equal(employeeActivityId, approved.EmployeeActivityId);
        Assert.Equal(EmployeeActivityStatus.Approved, approved.Status);
        Assert.Equal(15, approved.PointsSnapshot);
        Assert.Equal(15, detail.PointsAchieved);
        Assert.Empty(detail.SelfReportedActivities);
    }

    [Fact]
    public async Task The_promoted_employee_cannot_approve_their_own_activity()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 100);
        int employeeActivityId = await scenario.AddReportedActivity(points: 15);

        HttpResponseMessage response = await scenario.Approve(scenario.EmployeeClient, employeeActivityId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, (await scenario.GetDetail()).PointsAchieved);
    }

    [Fact]
    public async Task A_planned_activity_the_employee_has_not_reported_cannot_be_approved()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 100);
        int employeeActivityId = await scenario.AddPlannedActivity(points: 15);

        HttpResponseMessage response = await scenario.Approve(scenario.Hr, employeeActivityId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, (await scenario.GetDetail()).PointsAchieved);
    }
}
