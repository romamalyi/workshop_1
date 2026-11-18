using LeoHR.Server.Features.Activities;
using LeoHR.Server.Features.EmployeeActivities;
using LeoHR.Server.Features.PromotionCriteria;
using LeoHR.Server.Features.Promotions;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Employees;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SharedCriteriaDto = LeoHR.Server.Shared.PromotionCriteria.PromotionCriteriaDto;

namespace LeoHR.Server.IntegrationTests.Promotions;

// A Junior .NET employee with an open promotion to Strong Junior, whose criteria leave only the activity
// score open: English is met at creation, and nothing else is required.
public sealed class PromotionScenario
{
    public const string TargetLevel = "Strong Junior";

    private static readonly DateOnly accomplishedOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7);

    private readonly LeoHrApiFactory factory;

    private PromotionScenario(
        LeoHrApiFactory factory, HttpClient hr, HttpClient employeeClient, ApiEmployee employee, int promotionId)
    {
        this.factory = factory;
        Hr = hr;
        EmployeeClient = employeeClient;
        Employee = employee;
        PromotionId = promotionId;
    }

    public HttpClient Hr { get; }

    public HttpClient EmployeeClient { get; }

    public ApiEmployee Employee { get; }

    public int PromotionId { get; }

    public static async Task<PromotionScenario> Create(
        LeoHrApiFactory factory, int requiredScore, KeRequirement keRequirement = KeRequirement.None)
    {
        HttpClient hr = factory.ClientFor(TestUser.HrAdmin(factory.Employees.Add().Id));
        await SetTargetLevelCriteria(hr, requiredScore, keRequirement);

        ApiEmployee employee = factory.Employees.Add(
            profile: ".NET Developer", seniority: "Junior", englishLevel: "Advanced");
        PromotionDto promotion = await (await hr.PostJson("api/promotions", new CreatePromotionRequest(
            employee.Id, FromRole: string.Empty, TargetLevel, Technology.Dotnet, PromotionHolderId: string.Empty)))
            .ReadOk<PromotionDto>();

        return new PromotionScenario(
            factory, hr, factory.ClientFor(TestUser.Employee(employee.Id)), employee, promotion.Id);
    }

    public async Task<int> CreateCatalogActivity(int points)
    {
        ActivityDto activity = await (await Hr.PostJson("api/activities", new CreateActivityRequest(
            ActivityCategory.SelfCourses, $"Test course {Guid.NewGuid():N}", ActivityPeriod.TwelveMonths, points,
            Technology: null))).ReadOk<ActivityDto>();
        return activity.Id;
    }

    // HR plans the activity on the promotion; the employee then reports it as done.
    public async Task<int> AddReportedActivity(int points)
    {
        int employeeActivityId = await AddPlannedActivity(points);
        await (await EmployeeClient.PutJson(
            $"api/employees/{Employee.Id}/activities/{employeeActivityId}/self-report",
            new SelfReportRequest(accomplishedOn, Note: null))).EnsureSuccess();
        return employeeActivityId;
    }

    public async Task<int> AddPlannedActivity(int points)
    {
        int activityId = await CreateCatalogActivity(points);
        PromotionActivityDto added = await (await Hr.PostJson(
            $"api/promotions/{PromotionId}/activities", new AddActivityRequest(activityId, Note: null)))
            .ReadOk<PromotionActivityDto>();
        return added.EmployeeActivityId;
    }

    public Task<HttpResponseMessage> Approve(HttpClient client, int employeeActivityId)
    {
        return client.PatchJson(
            $"api/promotions/{PromotionId}/activities/{employeeActivityId}/approve",
            new ApproveActivityRequest(ApprovedById: null, accomplishedOn));
    }

    public Task<HttpResponseMessage> RevertApproval(int employeeActivityId)
    {
        return Hr.PatchAsync($"api/promotions/{PromotionId}/activities/{employeeActivityId}/revert-approval", null);
    }

    public Task<HttpResponseMessage> SetCriterion(string label, bool met)
    {
        return Hr.PutJson($"api/promotions/{PromotionId}/criteria", new SetCriterionRequest(label, met, Comment: null));
    }

    // Straight to the database: the API refuses a hand mark on the score, but stored rows can still carry one.
    public async Task StoreHandMarkedScore()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        PromotionEntity promotion = await db.Promotions.FindAsync(PromotionId) ?? throw new InvalidOperationException();
        promotion.ApprovedCriteriaLabels = [.. promotion.ApprovedCriteriaLabels, CriteriaLabels.ActivityScore];
        promotion.CriteriaOverrides = [new CriterionOverride(CriteriaLabels.ActivityScore, "Agreed with the manager")];
        promotion.Status = PromotionStatus.Ready;
        await db.SaveChangesAsync();
    }

    public async Task<PromotionEntity> GetStoredPromotion()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LeoHrDbContext db = scope.ServiceProvider.GetRequiredService<LeoHrDbContext>();
        return await db.Promotions.AsNoTracking().SingleAsync(p => p.Id == PromotionId);
    }

    public Task<PromotionDetailDto> GetDetail()
    {
        return Hr.GetOk<PromotionDetailDto>($"api/promotions/{PromotionId}");
    }

    // Criteria are seeded for every level, so the target level's row is edited in place rather than created.
    private static async Task SetTargetLevelCriteria(HttpClient hr, int requiredScore, KeRequirement keRequirement)
    {
        List<SharedCriteriaDto> all = await hr.GetOk<List<SharedCriteriaDto>>("api/promotion-criteria?technology=Dotnet");
        SharedCriteriaDto target = all.Single(c => c.Seniority == nameof(Seniority.Junior) && c.SubLevel == nameof(SubLevel.Strong));

        PromotionCriteriaRequest request = new(
            Technology.Dotnet, Seniority.Junior, SubLevel.Strong,
            WorkExperienceFromMonths: null, WorkExperienceToMonths: null, RequiredExperience: null,
            EnglishLevel.IntermediateLow, RequiredActivityId: null, keRequirement, requiredScore);
        await (await hr.PutJson($"api/promotion-criteria/{target.Id}?mode=Hard", request)).EnsureSuccess();
    }
}
