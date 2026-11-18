using System.Net;

using LeoHR.Server.Features.EmployeeActivities;
using LeoHR.Server.Features.KnowledgeEvaluations;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;
using LeoHR.Server.Shared.Models.Filter;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionReadAccessTests(LeoHrApiFactory factory)
{
    public static TheoryData<string> Reads =>
    [
        "detail",
        "active",
        "list",
        "knowledge-evaluation",
    ];

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task The_PM_can_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);

        HttpResponseMessage response = await chain.Pm.GetAsync(Url(read, chain));

        AssertAllowed(response);
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task The_Delivery_can_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);

        HttpResponseMessage response = await chain.Delivery.GetAsync(Url(read, chain));

        AssertAllowed(response);
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task HR_and_the_employee_can_still_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);

        AssertAllowed(await chain.Scenario.Hr.GetAsync(Url(read, chain)));
        AssertAllowed(await chain.Scenario.EmployeeClient.GetAsync(Url(read, chain)));
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task An_unrelated_user_cannot_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);

        HttpResponseMessage response = await chain.Stranger.GetAsync(Url(read, chain));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task Without_a_ManagerId_neither_the_PM_nor_the_Delivery_can_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);
        factory.Employees.SetManager(chain.Scenario.Employee.Id, managerId: null);

        Assert.Equal(HttpStatusCode.Forbidden, (await chain.Pm.GetAsync(Url(read, chain))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await chain.Delivery.GetAsync(Url(read, chain))).StatusCode);
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task Without_the_ManagerId_of_the_PM_the_Delivery_cannot_read_the_promotion(string read)
    {
        Chain chain = await Chain.Create(factory);
        factory.Employees.SetManager(chain.PmId, managerId: null);

        AssertAllowed(await chain.Pm.GetAsync(Url(read, chain)));
        Assert.Equal(HttpStatusCode.Forbidden, (await chain.Delivery.GetAsync(Url(read, chain))).StatusCode);
    }

    [Fact]
    public async Task The_PM_listing_an_employees_promotions_gets_that_promotion()
    {
        Chain chain = await Chain.Create(factory);

        PagedResult<PromotionDto> page = await chain.Pm.GetOk<PagedResult<PromotionDto>>(Url("list", chain));

        PromotionDto promotion = Assert.Single(page.Items);
        Assert.Equal(chain.Scenario.PromotionId, promotion.Id);
        Assert.Equal(chain.Scenario.Employee.Id, promotion.EmployeeId);
    }

    [Fact]
    public async Task Listing_without_an_employeeId_still_returns_only_your_own_promotions()
    {
        Chain chain = await Chain.Create(factory);

        PagedResult<PromotionDto> page = await chain.Pm.GetOk<PagedResult<PromotionDto>>("api/promotions");

        Assert.DoesNotContain(page.Items, p => p.Id == chain.Scenario.PromotionId);
    }

    [Fact]
    public async Task The_PM_and_the_Delivery_cannot_write_to_the_promotion()
    {
        Chain chain = await Chain.Create(factory);
        int activityId = await chain.Scenario.CreateCatalogActivity(points: 10);
        int plannedId = await chain.Scenario.AddPlannedActivity(points: 10);
        int promotionId = chain.Scenario.PromotionId;
        string employeeId = chain.Scenario.Employee.Id;

        foreach (HttpClient client in new[] { chain.Pm, chain.Delivery })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostJson(
                $"api/promotions/{promotionId}/activities", new AddActivityRequest(activityId, Note: null))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.DeleteAsync(
                $"api/promotions/{promotionId}/activities/{plannedId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PutJson(
                $"api/employees/{employeeId}/activities/{plannedId}/self-report",
                new SelfReportRequest(DateOnly.FromDateTime(DateTime.UtcNow), Note: null))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchJson(
                $"api/promotions/{promotionId}/activities/{plannedId}/approve",
                new ApproveActivityRequest(ApprovedById: null, DateOnly.FromDateTime(DateTime.UtcNow)))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchJson(
                $"api/promotions/{promotionId}", new UpdatePromotionRequest(PromotionHolderId: "any", ToRole: "any"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostJson(
                $"api/knowledge-evaluations/promotion/{promotionId}",
                new RequestKnowledgeEvaluationRequest(
                    DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), KnowledgeEvaluationOption.OnDate))).StatusCode);
        }

        Assert.Single((await chain.Scenario.GetDetail()).PlannedActivities);
    }

    private static void AssertAllowed(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"Expected a success status but got {(int)response.StatusCode}");
    }

    private static string Url(string read, Chain chain)
    {
        return read switch
        {
            "detail" => $"api/promotions/{chain.Scenario.PromotionId}",
            "active" => $"api/promotions/active/{chain.Scenario.Employee.Id}",
            "list" => $"api/promotions?employeeId={chain.Scenario.Employee.Id}",
            "knowledge-evaluation" => $"api/knowledge-evaluations/promotion/{chain.Scenario.PromotionId}",
            _ => throw new ArgumentOutOfRangeException(nameof(read), read, null),
        };
    }

    private sealed record Chain(
        PromotionScenario Scenario, string PmId, HttpClient Pm, HttpClient Delivery, HttpClient Stranger)
    {
        public static async Task<Chain> Create(LeoHrApiFactory factory)
        {
            PromotionScenario scenario = await PromotionScenario.Create(factory, requiredScore: 100);
            ApiEmployee delivery = factory.Employees.Add();
            ApiEmployee pm = factory.Employees.Add(managerId: delivery.Id);
            ApiEmployee stranger = factory.Employees.Add();
            factory.Employees.SetManager(scenario.Employee.Id, pm.Id);

            return new Chain(
                scenario,
                pm.Id,
                factory.ClientFor(TestUser.Employee(pm.Id)),
                factory.ClientFor(TestUser.Employee(delivery.Id)),
                factory.ClientFor(TestUser.Employee(stranger.Id)));
        }
    }
}
