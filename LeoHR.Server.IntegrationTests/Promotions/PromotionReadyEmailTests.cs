using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;

using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionReadyEmailTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task HR_is_emailed_when_a_promotion_becomes_ready_to_send()
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario();
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();

        await NotifyReadyPromotions();

        SentEmail email = Assert.Single(EmailsTo(hrManager));
        Assert.Contains($"{scenario.Employee.FirstName} {scenario.Employee.LastName}", email.Body);
        Assert.Contains(PromotionScenario.TargetLevel, email.Body);
        Assert.Contains($"/promotions/{scenario.PromotionId}", email.Body);
    }

    [Fact]
    public async Task A_promotion_back_at_ready_to_send_does_not_email_HR_again()
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario();
        int activityId = await scenario.AddReportedActivity(points: 24);
        await (await scenario.Approve(scenario.Hr, activityId)).EnsureSuccess();
        await NotifyReadyPromotions();

        await (await scenario.RevertApproval(activityId)).EnsureSuccess();
        Assert.Equal(PromotionStatus.InProgress, (await scenario.GetDetail()).Status);
        await (await scenario.Approve(scenario.Hr, activityId)).EnsureSuccess();
        Assert.Equal(PromotionStatus.Ready, (await scenario.GetDetail()).Status);
        await NotifyReadyPromotions();

        Assert.Single(EmailsTo(hrManager));
    }

    [Fact]
    public async Task A_promotion_in_progress_does_not_email_HR()
    {
        (PromotionScenario scenario, ApiEmployee hrManager) = await CreateScenario();
        await scenario.AddReportedActivity(points: 24);

        await NotifyReadyPromotions();

        Assert.Empty(EmailsTo(hrManager));
    }

    private async Task<(PromotionScenario Scenario, ApiEmployee HrManager)> CreateScenario()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        ApiEmployee hrManager = factory.Employees.Add();
        factory.Employees.SetHrManager(scenario.Employee.Id, hrManager.Id);
        return (scenario, hrManager);
    }

    private async Task NotifyReadyPromotions()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPromotionsService>().NotifyReadyPromotions();
    }

    private List<SentEmail> EmailsTo(ApiEmployee recipient)
    {
        return [.. factory.Smtp.Sent.Where(e => e.Recipients.Contains(recipient.Email))];
    }
}
