using System.Net;

using LeoHR.Server.Features.OnboardingFlows.Dto;
using LeoHR.Server.Features.OnboardingSettings.Dto;
using LeoHR.Server.Features.OnboardingTypes.Dto;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Employees;

namespace LeoHR.Server.IntegrationTests.Onboarding;

[Collection(ApiCollection.Name)]
public sealed class OnboardingFlowTests(LeoHrApiFactory factory)
{
    // "HR Assistant" is the seeded allowed position for a flow's responsible person.
    private const string ResponsiblePosition = "HR Assistant";
    private const string OfficeManager = "Office manager";
    private const string SystemAdministrator = "System Administrator";

    private readonly HttpClient admin = factory.ClientFor(TestUser.OnboardingAdmin(factory.Employees.Add().Id));

    [Fact]
    public async Task A_new_flow_gets_a_task_per_template_assigned_to_the_role_holder_or_left_empty()
    {
        Scenario s = await CreateScenario();

        Assert.Equal(3, s.Flow.Tasks.Count);
        Assert.Equal(s.Responsible.Id, s.Task("hr_assistant").AssigneeId);
        Assert.Equal(s.OfficeManagerDefault.Id, s.Task("office_manager").AssigneeId);
        Assert.Null(s.Task("system_administrator").AssigneeId);
        Assert.All(s.Flow.Tasks, t => Assert.Equal(OnboardingTaskStatus.NotStarted, t.Status));
    }

    [Fact]
    public async Task A_role_whose_default_person_is_inactive_leaves_its_task_unassigned()
    {
        Scenario s = await CreateScenario(officeManagerDefaultInactive: true);

        Assert.Null(s.Task("office_manager").AssigneeId);
    }

    [Fact]
    public async Task Recheck_does_not_offer_an_unassigned_task_to_an_inactive_role_default()
    {
        Scenario s = await CreateScenario();
        ApiEmployee sysAdmin = factory.Employees.Add(position: SystemAdministrator);
        await (await admin.PutJson("api/onboarding/settings/responsible-roles", new SaveResponsibleRolesRequest(
            [new RoleMappingInput("system_administrator", sysAdmin.Id)]))).EnsureSuccess();
        factory.Employees.Deactivate(sysAdmin.Id);

        List<RecheckAssignmentDto> offered = await admin.GetOk<List<RecheckAssignmentDto>>("api/onboarding/flows/unassigned-tasks/recheck");

        Assert.DoesNotContain(offered, a => a.TaskId == s.Task("system_administrator").Id);
    }

    [Fact]
    public async Task Any_onboarding_writer_can_fill_an_empty_assignee()
    {
        Scenario s = await CreateScenario();
        ApiEmployee sysAdmin = factory.Employees.Add(position: SystemAdministrator);

        HttpResponseMessage response = await Assign(s.OtherWriterClient, s.Task("system_administrator").Id, sysAdmin.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(sysAdmin.Id, (await GetFlow(s.Flow.Id)).Tasks.Single(t => t.ResponsibleRoleKey == "system_administrator").AssigneeId);
    }

    [Fact]
    public async Task A_writer_who_is_not_the_flows_responsible_person_cannot_replace_an_assignee()
    {
        Scenario s = await CreateScenario();
        ApiEmployee otherOfficeManager = factory.Employees.Add(position: OfficeManager);

        HttpResponseMessage response = await Assign(s.OtherWriterClient, s.Task("office_manager").Id, otherOfficeManager.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_responsible_person_replacing_an_assignee_resets_a_started_task_to_not_started()
    {
        Scenario s = await CreateScenario();
        int taskId = s.Task("office_manager").Id;
        await (await factory.ClientFor(TestUser.Employee(s.OfficeManagerDefault.Id))
            .PostAsync($"api/onboarding/tasks/{taskId}/start", content: null)).EnsureSuccess();
        ApiEmployee otherOfficeManager = factory.Employees.Add(position: OfficeManager);

        HttpResponseMessage response = await Assign(s.ResponsibleClient, taskId, otherOfficeManager.Id);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        OnboardingFlowTaskDto task = (await GetFlow(s.Flow.Id)).Tasks.Single(t => t.Id == taskId);
        Assert.Equal(otherOfficeManager.Id, task.AssigneeId);
        Assert.Equal(OnboardingTaskStatus.NotStarted, task.Status);
    }

    [Fact]
    public async Task A_task_can_only_go_to_someone_who_holds_its_role()
    {
        Scenario s = await CreateScenario();
        ApiEmployee notAnOfficeManager = factory.Employees.Add(position: SystemAdministrator);

        HttpResponseMessage response = await Assign(s.ResponsibleClient, s.Task("office_manager").Id, notAnOfficeManager.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_done_task_cannot_be_reassigned_even_by_an_admin()
    {
        Scenario s = await CreateScenario();
        int taskId = s.Task("office_manager").Id;
        await (await factory.ClientFor(TestUser.Employee(s.OfficeManagerDefault.Id))
            .PutAsync($"api/onboarding/tasks/{taskId}/done", content: null)).EnsureSuccess();
        ApiEmployee otherOfficeManager = factory.Employees.Add(position: OfficeManager);

        HttpResponseMessage response = await Assign(admin, taskId, otherOfficeManager.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> Assign(HttpClient client, int taskId, string assigneeId)
    {
        return client.PutJson($"api/onboarding/flows/tasks/{taskId}/assignee", new AssignTaskRequest(assigneeId));
    }

    private Task<OnboardingFlowDto> GetFlow(int id)
    {
        return admin.GetOk<OnboardingFlowDto>($"api/onboarding/flows/{id}");
    }

    // A type with three templates: one for the responsible person's own role, one for a role with a default
    // employee, and one for a role with no default.
    private async Task<Scenario> CreateScenario(bool officeManagerDefaultInactive = false)
    {
        ApiEmployee responsible = factory.Employees.Add(position: ResponsiblePosition);
        ApiEmployee otherWriter = factory.Employees.Add(position: ResponsiblePosition);
        ApiEmployee officeManagerDefault = factory.Employees.Add(position: OfficeManager);

        await (await admin.PutJson("api/onboarding/settings/responsible-roles", new SaveResponsibleRolesRequest(
        [
            new RoleMappingInput("office_manager", officeManagerDefault.Id),
            new RoleMappingInput("system_administrator", DefaultEmployeeId: null),
        ]))).EnsureSuccess();
        if (officeManagerDefaultInactive)
        {
            factory.Employees.Deactivate(officeManagerDefault.Id);
        }

        List<int> templateIds =
        [
            await OnboardingSetup.CreateTemplate(admin, "hr_assistant"),
            await OnboardingSetup.CreateTemplate(admin, "office_manager"),
            await OnboardingSetup.CreateTemplate(admin, "system_administrator"),
        ];
        OnboardingTypeDto type = await (await admin.PostJson(
            "api/onboarding/types", OnboardingSetup.TypeRequest($"Test type {Guid.NewGuid():N}", templateIds)))
            .ReadOk<OnboardingTypeDto>();

        OnboardingFlowDto flow = await (await admin.PostJson("api/onboarding/flows", new CreateOnboardingFlowRequest(
            "New Hire", Position: null, EmploymentType.FullTime, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14),
            Level: null, responsible.Id, type.Id, OptionalTemplateIds: null))).ReadOk<OnboardingFlowDto>();

        return new Scenario(
            flow,
            responsible,
            officeManagerDefault,
            factory.ClientFor(TestUser.OnboardingWriter(responsible.Id)),
            factory.ClientFor(TestUser.OnboardingWriter(otherWriter.Id)));
    }

    private sealed record Scenario(
        OnboardingFlowDto Flow,
        ApiEmployee Responsible,
        ApiEmployee OfficeManagerDefault,
        HttpClient ResponsibleClient,
        HttpClient OtherWriterClient)
    {
        public OnboardingFlowTaskDto Task(string roleKey)
        {
            return Flow.Tasks.Single(t => t.ResponsibleRoleKey == roleKey);
        }
    }
}
