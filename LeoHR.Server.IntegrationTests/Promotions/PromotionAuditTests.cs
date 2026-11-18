using LeoHR.Server.Features.KnowledgeEvaluations;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionAuditTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task A_successful_knowledge_evaluation_records_who_changed_the_promotion_and_when()
    {
        PromotionScenario scenario = await PromotionScenario.Create(
            factory, requiredScore: 60, KeRequirement.Junior);
        KnowledgeEvaluationDto evaluation = await (await scenario.EmployeeClient.PostJson(
            $"api/knowledge-evaluations/promotion/{scenario.PromotionId}",
            new RequestKnowledgeEvaluationRequest(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), KnowledgeEvaluationOption.OnDate)))
            .ReadOk<KnowledgeEvaluationDto>();
        PromotionEntity before = await scenario.GetStoredPromotion();
        string evaluatorId = factory.Employees.Add().Id;

        await (await factory.ClientFor(TestUser.HrAdmin(evaluatorId)).PatchJson(
            $"api/knowledge-evaluations/{evaluation.Id}",
            new UpdateKnowledgeEvaluationRequest(KnowledgeEvaluationStatus.Successful))).EnsureSuccess();

        PromotionEntity after = await scenario.GetStoredPromotion();
        Assert.Contains(CriteriaLabels.KeRequired, after.ApprovedCriteriaLabels);
        Assert.Equal(evaluatorId, after.UpdatedBy);
        Assert.True(after.UpdatedAt > before.UpdatedAt);
    }

    [Fact]
    public async Task Marking_a_criterion_as_met_by_hand_saves_the_tick_and_the_comment()
    {
        PromotionScenario scenario = await PromotionScenario.Create(
            factory, requiredScore: 60, KeRequirement.Junior);

        await (await scenario.Hr.PutJson($"api/promotions/{scenario.PromotionId}/criteria",
            new SetCriterionRequest(CriteriaLabels.KeRequired, Met: true, Comment: "Passed last year"))).EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.Contains(CriteriaLabels.KeRequired, detail.ApprovedCriteriaLabels!);
        Assert.Contains(detail.CriteriaOverrides!,
            o => o.Label == CriteriaLabels.KeRequired && o.Text == "Passed last year");
    }

    [Fact]
    public async Task Unmarking_a_criterion_saves_the_removed_tick_and_comment()
    {
        PromotionScenario scenario = await PromotionScenario.Create(
            factory, requiredScore: 60, KeRequirement.Junior);
        await (await scenario.SetCriterion(CriteriaLabels.KeRequired, met: true)).EnsureSuccess();

        await (await scenario.SetCriterion(CriteriaLabels.KeRequired, met: false)).EnsureSuccess();

        PromotionDetailDto detail = await scenario.GetDetail();
        Assert.DoesNotContain(CriteriaLabels.KeRequired, detail.ApprovedCriteriaLabels!);
        Assert.DoesNotContain(detail.CriteriaOverrides!, o => o.Label == CriteriaLabels.KeRequired);
    }
}
