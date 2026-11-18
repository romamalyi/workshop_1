using System.Net;

using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionCriteriaAutoTickTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task A_new_promotion_has_the_met_English_level_ticked_and_the_score_open()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);

        PromotionDetailDto detail = await scenario.GetDetail();

        Assert.Equal(PromotionStatus.InProgress, detail.Status);
        Assert.Contains(CriteriaLabels.EnglishLevel, detail.ApprovedCriteriaLabels!);
        Assert.DoesNotContain(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
    }

    [Fact]
    public async Task An_approval_below_the_required_score_leaves_the_score_unticked()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);

        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 12))).EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.Equal(12, detail.PointsAchieved);
        Assert.DoesNotContain(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.Equal(PromotionStatus.InProgress, detail.Status);
    }

    [Fact]
    public async Task The_approval_that_reaches_the_required_score_ticks_it_and_makes_the_promotion_Ready()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 12))).EnsureSuccess();

        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 12))).EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.Equal(24, detail.PointsAchieved);
        Assert.Contains(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.Equal(PromotionStatus.Ready, detail.Status);
    }

    [Fact]
    public async Task HR_cannot_mark_the_activity_score_as_met_by_hand()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);

        HttpResponseMessage response = await scenario.SetCriterion(CriteriaLabels.ActivityScore, met: true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.DoesNotContain(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.Equal(PromotionStatus.InProgress, detail.Status);
    }

    [Fact]
    public async Task HR_cannot_untick_a_met_activity_score_by_hand()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();

        HttpResponseMessage response = await scenario.SetCriterion(CriteriaLabels.ActivityScore, met: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.Contains(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.Equal(PromotionStatus.Ready, detail.Status);
    }

    [Fact]
    public async Task Reverting_an_approval_that_drops_the_points_below_the_score_unticks_it_and_returns_to_In_Progress()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        int employeeActivityId = await scenario.AddReportedActivity(points: 24);
        await (await scenario.Approve(scenario.Hr, employeeActivityId)).EnsureSuccess();

        await (await scenario.RevertApproval(employeeActivityId)).EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.Equal(0, detail.PointsAchieved);
        Assert.DoesNotContain(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.Equal(PromotionStatus.InProgress, detail.Status);
    }

    [Fact]
    public async Task Opening_a_promotion_whose_score_was_marked_by_hand_with_short_points_unticks_it()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);
        await scenario.StoreHandMarkedScore();

        await (await scenario.Hr.PutAsync($"api/promotions/{scenario.PromotionId}/criteria-verification", null))
            .EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.DoesNotContain(CriteriaLabels.ActivityScore, detail.ApprovedCriteriaLabels!);
        Assert.DoesNotContain(detail.CriteriaOverrides!, o => o.Label == CriteriaLabels.ActivityScore);
        Assert.Equal(PromotionStatus.InProgress, detail.Status);
    }

    [Fact]
    public async Task Sending_a_promotion_whose_score_was_marked_by_hand_with_short_points_is_refused()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 60);
        await scenario.StoreHandMarkedScore();

        HttpResponseMessage response =
            await scenario.Hr.PostJson($"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PromotionStatus.InProgress, (await scenario.GetDetail()).Status);
    }
}
