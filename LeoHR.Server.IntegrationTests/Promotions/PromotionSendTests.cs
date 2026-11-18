using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionSendTests(LeoHrApiFactory factory)
{
    [Fact]
    public async Task Two_clicks_on_Send_send_one_email_and_create_one_next_promotion()
    {
        PromotionScenario scenario = await CreateReadyScenario();
        int emailsBefore = factory.Smtp.Sent.Count;

        HttpResponseMessage[] responses;
        factory.Smtp.Delay = TimeSpan.FromMilliseconds(500);
        try
        {
            responses = await Task.WhenAll(Send(scenario.Hr, scenario), Send(scenario.Hr, scenario));
        }
        finally
        {
            factory.Smtp.Delay = TimeSpan.Zero;
        }

        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Equal(1, factory.Smtp.Sent.Count - emailsBefore);
        Assert.Equal(PromotionStatus.Sent, (await scenario.GetStoredPromotion()).Status);
        Assert.Equal(1, await CountOpenPromotions(scenario.Employee.Id));
    }

    [Fact]
    public async Task A_send_that_fails_after_the_email_went_out_is_finished_by_the_retry_without_a_second_email()
    {
        PromotionScenario scenario = await CreateReadyScenario();
        int emailsBefore = factory.Smtp.Sent.Count;

        await using (WebApplicationFactory<Program> failing = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped<IEmployeeLevelStart, FailingLevelStart>())))
        {
            HttpClient hr = failing.CreateClient();
            hr.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, TestUser.HrAdmin(factory.Employees.Add().Id).ToHeader());
            await Assert.ThrowsAnyAsync<Exception>(async () => await (await Send(hr, scenario)).EnsureSuccess());
        }

        PromotionEntity afterFailure = await scenario.GetStoredPromotion();
        Assert.Equal(1, factory.Smtp.Sent.Count - emailsBefore);
        Assert.Equal(PromotionStatus.Ready, afterFailure.Status);
        Assert.NotNull(afterFailure.EmailSentAt);

        await (await Send(scenario.Hr, scenario)).EnsureSuccess();

        Assert.Equal(1, factory.Smtp.Sent.Count - emailsBefore);
        Assert.Equal(PromotionStatus.Sent, (await scenario.GetStoredPromotion()).Status);
        Assert.Equal(1, await CountOpenPromotions(scenario.Employee.Id));
    }

    private async Task<PromotionScenario> CreateReadyScenario()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 20);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();
        Assert.Equal(PromotionStatus.Ready, (await scenario.GetDetail()).Status);
        return scenario;
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, PromotionScenario scenario)
    {
        return client.PostJson($"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest());
    }

    private async Task<int> CountOpenPromotions(string employeeId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        return await db.Promotions.CountAsync(p => p.EmployeeId == employeeId
            && (p.Status == PromotionStatus.InProgress || p.Status == PromotionStatus.Ready));
    }

    private sealed class FailingLevelStart : IEmployeeLevelStart
    {
        public Task SetLevelStartDateAsync(string employeeId, DateTime date)
        {
            throw new InvalidOperationException("Simulated failure after the email went out.");
        }
    }
}
