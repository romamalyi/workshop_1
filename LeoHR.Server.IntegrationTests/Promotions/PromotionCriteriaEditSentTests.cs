using LeoHR.Server.Features.PromotionCriteria;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;

using SharedCriteriaDto = LeoHR.Server.Shared.PromotionCriteria.PromotionCriteriaDto;

namespace LeoHR.Server.IntegrationTests.Promotions;

[Collection(ApiCollection.Name)]
public sealed class PromotionCriteriaEditSentTests(LeoHrApiFactory factory)
{
    private const int OldScore = 20;
    private const int NewScore = 50;

    [Theory]
    [InlineData("Hard")]
    [InlineData("Soft")]
    public async Task Editing_the_criteria_leaves_a_Sent_promotion_on_its_old_values_and_ticks(string mode)
    {
        PromotionScenario scenario = await CreateSentScenario();
        PromotionDetailDto before = await scenario.GetDetail();

        await EditScore(scenario.Hr, Seniority.Junior, SubLevel.Strong, NewScore, mode);

        PromotionDetailDto after = await scenario.GetDetail();
        Assert.Equal(PromotionStatus.Sent, after.Status);
        Assert.Equal(OldScore, after.Criteria!.ActivitiesScore);
        Assert.Equal(before.ApprovedCriteriaLabels, after.ApprovedCriteriaLabels);
    }

    [Fact]
    public async Task Hard_edit_gives_open_promotions_of_the_level_the_new_values_and_leaves_the_Sent_one()
    {
        PromotionScenario sent = await CreateSentScenario();
        int openPromotionId = await CreateOpenPromotion(sent.Hr);

        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore, "Hard");

        PromotionDetailDto open = await sent.Hr.GetOk<PromotionDetailDto>($"api/promotions/{openPromotionId}");
        Assert.Equal(NewScore, open.Criteria!.ActivitiesScore);
        Assert.Equal(OldScore, (await sent.GetDetail()).Criteria!.ActivitiesScore);
    }

    [Fact]
    public async Task A_promotion_created_after_a_Hard_edit_gets_the_new_values()
    {
        PromotionScenario sent = await CreateSentScenario();

        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore, "Hard");
        int createdAfterId = await CreateOpenPromotion(sent.Hr);

        PromotionDetailDto created = await sent.Hr.GetOk<PromotionDetailDto>($"api/promotions/{createdAfterId}");
        Assert.Equal(NewScore, created.Criteria!.ActivitiesScore);
    }

    [Fact]
    public async Task Hard_edit_of_another_level_leaves_a_Sent_promotion_unchanged()
    {
        PromotionScenario scenario = await CreateSentScenario();

        await EditScore(scenario.Hr, Seniority.Intermediate, SubLevel.None, NewScore, "Hard");

        Assert.Equal(OldScore, (await scenario.GetDetail()).Criteria!.ActivitiesScore);
    }

    [Fact]
    public async Task A_Hard_edit_then_a_Soft_edit_keeps_each_promotion_on_the_version_it_read()
    {
        PromotionScenario sent = await CreateSentScenario();
        int openId = await CreateOpenPromotion(sent.Hr);

        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore, "Hard");
        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore + 20, "Soft");
        int createdAfterId = await CreateOpenPromotion(sent.Hr);

        Assert.Equal(OldScore, (await sent.GetDetail()).Criteria!.ActivitiesScore);
        Assert.Equal(NewScore, (await GetDetail(sent.Hr, openId)).Criteria!.ActivitiesScore);
        Assert.Equal(NewScore + 20, (await GetDetail(sent.Hr, createdAfterId)).Criteria!.ActivitiesScore);
    }

    [Fact]
    public async Task A_Soft_edit_then_a_Hard_edit_keeps_each_promotion_on_the_version_it_read()
    {
        PromotionScenario sent = await CreateSentScenario();
        int openId = await CreateOpenPromotion(sent.Hr);

        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore, "Soft");
        await EditScore(sent.Hr, Seniority.Junior, SubLevel.Strong, NewScore + 20, "Hard");
        int createdAfterId = await CreateOpenPromotion(sent.Hr);

        Assert.Equal(OldScore, (await sent.GetDetail()).Criteria!.ActivitiesScore);
        Assert.Equal(OldScore, (await GetDetail(sent.Hr, openId)).Criteria!.ActivitiesScore);
        Assert.Equal(NewScore + 20, (await GetDetail(sent.Hr, createdAfterId)).Criteria!.ActivitiesScore);
    }

    private async Task<PromotionScenario> CreateSentScenario()
    {
        PromotionScenario scenario = await PromotionScenario.Create(factory, OldScore);
        await (await scenario.Approve(scenario.Hr, await scenario.AddReportedActivity(points: 24))).EnsureSuccess();
        await (await scenario.Hr.PostJson($"api/promotions/{scenario.PromotionId}/send", new SendPromotionRequest()))
            .EnsureSuccess();
        Assert.Equal(PromotionStatus.Sent, (await scenario.GetDetail()).Status);
        return scenario;
    }

    private async Task<int> CreateOpenPromotion(HttpClient hr)
    {
        ApiEmployee employee = factory.Employees.Add(
            profile: ".NET Developer", seniority: "Junior", englishLevel: "Advanced");
        PromotionDto promotion = await (await hr.PostJson("api/promotions", new CreatePromotionRequest(
            employee.Id, FromRole: string.Empty, PromotionScenario.TargetLevel, Technology.Dotnet, PromotionHolderId: string.Empty)))
            .ReadOk<PromotionDto>();
        return promotion.Id;
    }

    private static Task<PromotionDetailDto> GetDetail(HttpClient hr, int promotionId)
    {
        return hr.GetOk<PromotionDetailDto>($"api/promotions/{promotionId}");
    }

    private static async Task EditScore(HttpClient hr, Seniority seniority, SubLevel subLevel, int score, string mode)
    {
        List<SharedCriteriaDto> all = await hr.GetOk<List<SharedCriteriaDto>>("api/promotion-criteria?technology=Dotnet");
        SharedCriteriaDto row = all.Single(c => c.Seniority == seniority.ToString() && c.SubLevel == subLevel.ToString());

        PromotionCriteriaRequest request = new(
            Technology.Dotnet, seniority, subLevel, row.WorkExperienceFromMonths, row.WorkExperienceToMonths,
            row.RequiredExperience, Enum.Parse<EnglishLevel>(row.EnglishLevel), row.RequiredActivityId,
            Enum.Parse<KeRequirement>(row.KeRequired), score);
        await (await hr.PutJson($"api/promotion-criteria/{row.Id}?mode={mode}", request)).EnsureSuccess();
    }
}
