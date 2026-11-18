using System.Net;

using LeoHR.Server.Features.Notifications.Dto;
using LeoHR.Server.IntegrationTests.Infrastructure;

namespace LeoHR.Server.IntegrationTests.Notifications;

[Collection(ApiCollection.Name)]
public sealed class EnglishLevelChangedEventTests(LeoHrApiFactory factory)
{
    private readonly HttpClient englishApp = factory.ClientFor(TestUser.IntegrationClient("english-app"));

    [Fact]
    public async Task An_english_level_change_without_parameters_is_rejected_and_no_email_is_sent()
    {
        (var employee, var hrManager) = AddEmployeeWithHrManager();

        HttpResponseMessage response = await englishApp.PostJson("api/integrations/events", new EmployeeEventRequest(
            EmployeeEventType.EnglishLevelChanged, employee.Id, "English level changed from B1 to B2"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("currentLevel", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(factory.Smtp.Sent, mail => SentTo(mail, hrManager.Email, employee.Email));
    }

    [Fact]
    public async Task An_english_level_change_without_the_current_level_is_rejected()
    {
        (var employee, _) = AddEmployeeWithHrManager();

        HttpResponseMessage response = await englishApp.PostJson("api/integrations/events", new EmployeeEventRequest(
            EmployeeEventType.EnglishLevelChanged, employee.Id, "English level changed",
            new Dictionary<string, string> { ["previousLevel"] = "B1" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_english_level_change_with_parameters_emails_the_employee_with_the_english_template()
    {
        (var employee, _) = AddEmployeeWithHrManager();

        HttpResponseMessage response = await englishApp.PostJson("api/integrations/events", new EmployeeEventRequest(
            EmployeeEventType.EnglishLevelChanged, employee.Id, "English level changed from B1 to B2",
            new Dictionary<string, string> { ["currentLevel"] = "B2", ["previousLevel"] = "B1" }));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        SentEmail mail = Assert.Single(factory.Smtp.Sent, m => SentTo(m, employee.Email));
        Assert.Equal("Congratulations on Your New English Level!", mail.Subject);
    }

    private (Server.Shared.Employees.ApiEmployee Employee, Server.Shared.Employees.ApiEmployee HrManager) AddEmployeeWithHrManager()
    {
        var hrManager = factory.Employees.Add();
        return (factory.Employees.Add(hrManagerId: hrManager.Id), hrManager);
    }

    private static bool SentTo(SentEmail mail, params string[] addresses)
    {
        return mail.Recipients.Any(r => addresses.Contains(r, StringComparer.OrdinalIgnoreCase));
    }
}
