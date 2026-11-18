using System.Net;

using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionSendKnowledgeEvaluationTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task Sending_is_refused_while_a_knowledge_evaluation_is_planned()
    {
        PromotionScenario scenario = await ReadyPromotion();
        await StoreEvaluation(scenario, KnowledgeEvaluationStatus.Planned);

        HttpResponseMessage response = await Send(scenario);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(PromotionStatus.Ready, (await scenario.GetDetail()).Status);
    }

    [Theory]
    [InlineData(KnowledgeEvaluationStatus.Successful)]
    [InlineData(KnowledgeEvaluationStatus.Unsuccessful)]
    public async Task Sending_works_once_the_knowledge_evaluation_is_done(KnowledgeEvaluationStatus status)
    {
        PromotionScenario scenario = await ReadyPromotion();
        await StoreEvaluation(scenario, status);

        HttpResponseMessage response = await Send(scenario);

        await response.EnsureSuccess();
        Assert.Equal(PromotionStatus.Sent, (await scenario.GetDetail()).Status);
    }

    [Fact]
    public async Task Sending_ignores_a_planned_knowledge_evaluation_when_the_feature_is_off()
    {
        PromotionScenario scenario = await ReadyPromotion();
        await StoreEvaluation(scenario, KnowledgeEvaluationStatus.Planned);
        await using WebApplicationFactory<Program> featureOff = factory.WithWebHostBuilder(
            builder => builder.UseSetting("FeatureManagement:KnowledgeEvaluation", "false"));

        HttpResponseMessage response = await HrClient(featureOff).PostJson(
            $"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest());

        await response.EnsureSuccess();
        Assert.Equal(PromotionStatus.Sent, (await scenario.GetDetail()).Status);
    }

    [Theory]
    [InlineData(KnowledgeEvaluationStatus.Planned)]
    [InlineData(KnowledgeEvaluationStatus.Successful)]
    [InlineData(KnowledgeEvaluationStatus.Unsuccessful)]
    public async Task The_promotion_detail_carries_the_knowledge_evaluation_status(KnowledgeEvaluationStatus status)
    {
        PromotionScenario scenario = await ReadyPromotion();
        await StoreEvaluation(scenario, status);

        Assert.Equal(status, (await scenario.GetDetail()).KeStatus);
    }

    [Fact]
    public async Task The_promotion_detail_has_no_knowledge_evaluation_status_without_an_evaluation()
    {
        PromotionScenario scenario = await ReadyPromotion();

        Assert.Null((await scenario.GetDetail()).KeStatus);
    }

    [Fact]
    public async Task The_promotion_detail_has_no_knowledge_evaluation_status_when_the_feature_is_off()
    {
        PromotionScenario scenario = await ReadyPromotion();
        await StoreEvaluation(scenario, KnowledgeEvaluationStatus.Planned);
        await using WebApplicationFactory<Program> featureOff = factory.WithWebHostBuilder(
            builder => builder.UseSetting("FeatureManagement:KnowledgeEvaluation", "false"));

        PromotionDetailDto detail = await HrClient(featureOff).GetOk<PromotionDetailDto>(
            $"api/promotions/{scenario.PromotionId}");

        Assert.Null(detail.KeStatus);
    }

    private HttpClient HrClient(WebApplicationFactory<Program> host)
    {
        HttpClient hr = host.CreateClient();
        hr.DefaultRequestHeaders.Add(
            TestAuthHandler.HeaderName, TestUser.HrAdmin(factory.Employees.Add().Id).ToHeader());
        return hr;
    }

    private static Task<HttpResponseMessage> Send(PromotionScenario scenario)
    {
        return scenario.Hr.PostJson($"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest());
    }

    private async Task<PromotionScenario> ReadyPromotion()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();
        return scenario;
    }

    private async Task StoreEvaluation(PromotionScenario scenario, KnowledgeEvaluationStatus status)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        db.KnowledgeEvaluations.Add(new KnowledgeEvaluationEntity
        {
            PromotionId = scenario.PromotionId,
            RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = status,
        });
        await db.SaveChangesAsync();
    }
}
