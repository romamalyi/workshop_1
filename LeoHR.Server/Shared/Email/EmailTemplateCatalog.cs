namespace LeoHR.Server.Shared.Email;

public enum EmailTemplateCategory
{
    Promotions,
    EmployeeEvents,
}

public record EmailPlaceholder(string Token, string Meaning);

public record EmailTemplateInfo(
    string Id,
    EmailTemplateCategory Category,
    bool IsHtml,
    IReadOnlyList<EmailPlaceholder> Placeholders,
    IReadOnlyList<RecipientRole> AllowedRecipients);

// The templates LeoHR sends itself. The text and recipients are HR-editable in email_templates;
// what each template means (category, format, placeholders, allowed roles) is fixed here.
public static class EmailTemplateCatalog
{
    public const string Promotion = "promotion";
    public const string PromotionReady = "promotion_ready";
    public const string KnowledgeEvaluationRequested = "knowledge_evaluation_requested";
    public const string EmployeeEvent = "employee_event";
    public const string EnglishLevelChanged = "english_level_changed";
    public const string EnglishGroupAssigned = "english_group_assigned";

    private static readonly RecipientRole[] employeeRoles =
    [
        RecipientRole.Employee, RecipientRole.ProjectManager, RecipientRole.HrManager,
        RecipientRole.HrManagersManager, RecipientRole.ProjectManagersManager,
    ];

    private static readonly RecipientRole[] englishRoles = [.. employeeRoles, RecipientRole.EnglishTeacher];
    private static readonly RecipientRole[] promotionRoles = [.. employeeRoles, RecipientRole.PromotionHolder];

    public static readonly IReadOnlyList<EmailTemplateInfo> All =
    [
        new(Promotion, EmailTemplateCategory.Promotions, IsHtml: false,
            [
                new("{firstName}", "the employee's first name"),
                new("{lastName}", "the employee's last name"),
                new("{fromRole}", "the level they're promoted from"),
                new("{toRole}", "the level they're promoted to"),
            ],
            promotionRoles),
        new(PromotionReady, EmailTemplateCategory.Promotions, IsHtml: false,
            [
                new("{firstName}", "the employee's first name"),
                new("{lastName}", "the employee's last name"),
                new("{toRole}", "the level they're promoted to"),
                new("{promotionLink}", "the link to the promotion in LeoHR"),
            ],
            promotionRoles),
        new(KnowledgeEvaluationRequested, EmailTemplateCategory.Promotions, IsHtml: false,
            [
                new("{firstName}", "the employee's first name"),
                new("{lastName}", "the employee's last name"),
                new("{requestedDate}", "the requested date, e.g. \"on 12 Nov 2026\" or \"any time after 12 Nov 2026\""),
                new("{comment}", "the employee's comment"),
                new("{promotionLink}", "the link to the promotion in LeoHR"),
            ],
            promotionRoles),
        new(EnglishLevelChanged, EmailTemplateCategory.EmployeeEvents, IsHtml: true,
            [
                new("{employeeName}", "the employee's full name"),
                new("{currentLevel}", "the new English level"),
                new("{previousLevel}", "the previous English level"),
                new("{strengths}", "the list of strengths"),
                new("{improvementAreas}", "the list of areas to improve"),
            ],
            englishRoles),
        new(EnglishGroupAssigned, EmailTemplateCategory.EmployeeEvents, IsHtml: true,
            [
                new("{employeeName}", "the employee's full name"),
                new("{groupName}", "the English group"),
                new("{schedule}", "the class schedule"),
                new("{sessionsPerWeek}", "the number of sessions per week"),
                new("{teacherName}", "the group's teacher"),
            ],
            englishRoles),
        new(EmployeeEvent, EmailTemplateCategory.EmployeeEvents, IsHtml: false,
            [
                new("{employeeName}", "the employee's full name"),
                new("{eventType}", "the type of the event"),
                new("{eventDescription}", "what happened, as the reporting app describes it"),
            ],
            employeeRoles),
    ];

    public static EmailTemplateInfo? Find(string templateId)
    {
        return All.FirstOrDefault(t => t.Id == templateId);
    }
}
