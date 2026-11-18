using System.Net;
using System.Net.Http.Json;

using LeoHR.Server.Features.Activities;
using LeoHR.Server.Features.EmployeeActivities;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.IntegrationTests.Onboarding;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Results.Errors;

namespace LeoHR.Server.IntegrationTests;

// Every duplicate resource is 409 with errorCode DUPLICATE_RESOURCE; a repeated submission that a business
// rule forbids is not a duplicate resource and stays 400.
[Collection(ApiCollection.Name)]
public sealed class DuplicateHandlingTests(LeoHrApiFactory factory)
{
    private readonly HttpClient hr = factory.ClientFor(TestUser.HrAdmin(factory.Employees.Add().Id));

    [Fact]
    public async Task A_second_catalog_activity_with_the_same_name_is_a_409()
    {
        string name = $"Test course {Guid.NewGuid():N}";
        await (await CreateActivity(name, ActivityPeriod.TwelveMonths)).EnsureSuccess();

        HttpResponseMessage response = await CreateActivity(name.ToUpperInvariant(), ActivityPeriod.TwelveMonths);

        await AssertDuplicate(response);
    }

    [Fact]
    public async Task A_second_onboarding_type_with_the_same_name_is_a_409()
    {
        HttpClient onboardingAdmin = factory.ClientFor(TestUser.OnboardingAdmin(factory.Employees.Add().Id));
        int templateId = await OnboardingSetup.CreateTemplate(onboardingAdmin, roleKey: null);
        string name = $"Test type {Guid.NewGuid():N}";
        await (await onboardingAdmin.PostJson("api/onboarding/types", OnboardingSetup.TypeRequest(name, [templateId])))
            .EnsureSuccess();

        HttpResponseMessage response = await onboardingAdmin.PostJson(
            "api/onboarding/types", OnboardingSetup.TypeRequest(name.ToUpperInvariant(), [templateId]));

        await AssertDuplicate(response);
    }

    [Fact]
    public async Task Submitting_an_overall_active_activity_twice_is_a_business_rule_400_not_a_duplicate()
    {
        string employeeId = factory.Employees.Add().Id;
        ActivityDto activity = await (await CreateActivity($"Test certificate {Guid.NewGuid():N}", ActivityPeriod.OverallActive))
            .ReadOk<ActivityDto>();
        CreateEmployeeActivityRequest submission = new(
            activity.Id, EmployeeActivityStatus.SelfReported, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            Note: null, PromotionId: null, CreatedById: null);
        await (await hr.PostJson($"api/employees/{employeeId}/activities", submission)).EnsureSuccess();

        HttpResponseMessage response = await hr.PostJson($"api/employees/{employeeId}/activities", submission);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ErrorResponse body = (await response.Content.ReadFromJsonAsync<ErrorResponse>(HttpClientJson.Options))!;
        Assert.DoesNotContain(body.Errors, e => e.ErrorCode == "DUPLICATE_RESOURCE");
    }

    private Task<HttpResponseMessage> CreateActivity(string name, ActivityPeriod period)
    {
        return hr.PostJson("api/activities", new CreateActivityRequest(
            ActivityCategory.Certifications, name, period, Points: 10, Technology: null));
    }

    private static async Task AssertDuplicate(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ErrorResponse body = (await response.Content.ReadFromJsonAsync<ErrorResponse>(HttpClientJson.Options))!;
        Assert.Contains(body.Errors, e => e.ErrorCode == "DUPLICATE_RESOURCE");
    }
}
