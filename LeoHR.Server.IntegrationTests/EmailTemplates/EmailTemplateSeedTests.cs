using LeoHR.Server.Features.EmailTemplates;
using LeoHR.Server.IntegrationTests.Infrastructure;
using LeoHR.Server.Shared.Email;

namespace LeoHR.Server.IntegrationTests.EmailTemplates;

// The test database is created by migrations alone, the same way as a new developer's or a rebuilt environment's.
[Collection(ApiCollection.Name)]
public sealed class EmailTemplateSeedTests(LeoHrApiFactory factory)
{
    private readonly HttpClient hr = factory.ClientFor(TestUser.HrAdmin(factory.Employees.Add().Id));

    [Theory]
    [InlineData("promotion")]
    [InlineData("promotion_ready")]
    [InlineData("knowledge_evaluation_requested")]
    [InlineData("birthday")]
    [InlineData("welcome")]
    [InlineData("first-day")]
    [InlineData("documents")]
    [InlineData("tools")]
    [InlineData("checkin")]
    [InlineData("employee_event")]
    [InlineData("english_level_changed")]
    [InlineData("english_group_assigned")]
    public async Task A_new_database_has_the_email_template(string id)
    {
        EmailTemplateResponse template = await hr.GetOk<EmailTemplateResponse>($"api/email-templates/{id}");

        Assert.Equal(id, template.Id);
        Assert.NotEmpty(template.Subject);
        Assert.NotEmpty(template.Body);
    }

    [Fact]
    public async Task A_new_database_lists_the_promotion_email_with_its_recipients()
    {
        List<EmailTemplateSettingsResponse> templates = await hr.GetOk<List<EmailTemplateSettingsResponse>>(
            $"api/email-templates?category={EmailTemplateCategory.Promotions}");

        EmailTemplateSettingsResponse promotion = Assert.Single(templates, t => t.Id == EmailTemplateCatalog.Promotion);
        Assert.Equal(
            [RecipientRole.Employee, RecipientRole.ProjectManager, RecipientRole.HrManager, RecipientRole.PromotionHolder],
            promotion.ToRecipients);
        Assert.Equal([RecipientRole.HrManagersManager, RecipientRole.ProjectManagersManager], promotion.CcRecipients);
    }

    [Theory]
    [InlineData(EmailTemplateCatalog.PromotionReady, new[] { RecipientRole.HrManager })]
    [InlineData(EmailTemplateCatalog.KnowledgeEvaluationRequested, new[] { RecipientRole.HrManager, RecipientRole.PromotionHolder })]
    public async Task A_new_database_lists_the_HR_promotion_emails_with_their_recipients(string id, RecipientRole[] to)
    {
        List<EmailTemplateSettingsResponse> templates = await hr.GetOk<List<EmailTemplateSettingsResponse>>(
            $"api/email-templates?category={EmailTemplateCategory.Promotions}");

        EmailTemplateSettingsResponse template = Assert.Single(templates, t => t.Id == id);
        Assert.Equal(to, template.ToRecipients);
        Assert.Empty(template.CcRecipients);
    }
}
