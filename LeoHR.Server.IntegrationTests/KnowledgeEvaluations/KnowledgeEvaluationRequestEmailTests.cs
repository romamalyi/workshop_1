using System.Globalization;
using System.Net;

using LeoHR.Server.Features.KnowledgeEvaluations;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.IntegrationTests.Promotions;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LeoHR.Server.IntegrationTests.KnowledgeEvaluations;

[Collection(ApiCollection.Name)]
public sealed class KnowledgeEvaluationRequestEmailTests(LeoHrApiFactory factory)
{
    private static readonly DateOnly requestedDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
    private static readonly string formattedDate = requestedDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    [Theory]
    [InlineData(KnowledgeEvaluationOption.OnDate, "on")]
    [InlineData(KnowledgeEvaluationOption.AnyTimeAfter, "any time after")]
    public async Task A_request_emails_HR_and_the_promotion_holder(KnowledgeEvaluationOption option, string dateWording)
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario(KeRequirement.Junior);
        ApiEmployee holder = factory.Employees.Add();
        await (await scenario.Hr.PatchJson($"api/promotions/{scenario.PromotionId}",
            new UpdatePromotionRequest(holder.Id, PromotionScenario.TargetLevel))).EnsureSuccess();

        await (await Request(scenario.EmployeeClient, scenario, option, "Ready after the release")).EnsureSuccess();

        SentEmail email = Assert.Single(factory.Smtp.Sent, e => e.Recipients.Contains(hrManager.Email));
        Assert.Contains(holder.Email, email.Recipients);
        Assert.Contains($"{scenario.Employee.FirstName} {scenario.Employee.LastName}", email.Body);
        Assert.Contains($"{dateWording} {formattedDate}", email.Body);
        Assert.Contains("Ready after the release", email.Body);
        Assert.Contains($"/promotions/{scenario.PromotionId}", email.Body);
    }

    [Fact]
    public async Task A_request_on_a_promotion_without_a_holder_emails_the_default_holder()
    {
        (PromotionScenario scenario, _) = await CreateScenario(KeRequirement.Junior);
        Assert.Empty((await scenario.GetStoredPromotion()).PromotionHolderId);
        ApiEmployee defaultHolder = factory.Employees.Add(id: AppSettingsEntity.DefaultPromotionHolder);
        try
        {
            await (await Request(scenario.EmployeeClient, scenario)).EnsureSuccess();

            Assert.Contains(factory.Smtp.Sent, e => e.Recipients.Contains(defaultHolder.Email)
                && e.Body.Contains($"/promotions/{scenario.PromotionId}"));
        }
        finally
        {
            factory.Employees.Remove(defaultHolder.Id);
        }
    }

    [Fact]
    public async Task A_request_for_a_sent_promotion_is_rejected_without_an_email()
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario(KeRequirement.None);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();
        await (await scenario.Hr.PostJson(
            $"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest())).EnsureSuccess();

        HttpResponseMessage response = await Request(scenario.EmployeeClient, scenario);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("can't be planned for a sent promotion", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(factory.Smtp.Sent, e => e.Subject.Contains("knowledge evaluation")
            && e.Recipients.Contains(hrManager.Email));
    }

    [Fact]
    public async Task No_email_is_sent_while_knowledge_evaluation_is_turned_off()
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario(KeRequirement.Junior);
        await using WebApplicationFactory<Program> featureOff = factory.WithWebHostBuilder(
            builder => builder.UseSetting("FeatureManagement:KnowledgeEvaluation", "false"));
        HttpClient employee = featureOff.CreateClient();
        employee.DefaultRequestHeaders.Add(
            TestAuthHandler.HeaderName, TestUser.Employee(scenario.Employee.Id).ToHeader());

        HttpResponseMessage response = await Request(employee, scenario);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(factory.Smtp.Sent, e => e.Recipients.Contains(hrManager.Email));
    }

    private async Task<(PromotionScenario Scenario, ApiEmployee HrManager)> CreateScenario(KeRequirement keRequirement)
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20, keRequirement);
        ApiEmployee hrManager = factory.Employees.Add();
        factory.Employees.SetHrManager(scenario.Employee.Id, hrManager.Id);
        return (scenario, hrManager);
    }

    private static Task<HttpResponseMessage> Request(
        HttpClient client,
        PromotionScenario scenario,
        KnowledgeEvaluationOption option = KnowledgeEvaluationOption.OnDate,
        string? comment = null)
    {
        return client.PostJson(
            $"api/knowledge-evaluations/promotion/{scenario.PromotionId}",
            new RequestKnowledgeEvaluationRequest(requestedDate, option, comment));
    }
}
