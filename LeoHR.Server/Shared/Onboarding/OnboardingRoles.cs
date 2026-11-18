namespace LeoHR.Server.Shared.Onboarding;

public record OnboardingRoleDefinition(string Key, string ExactTitle);

public static class OnboardingRoles
{
    public static readonly OnboardingRoleDefinition HrManager = new("hr_manager", "HR Manager");

    public static readonly IReadOnlyList<OnboardingRoleDefinition> MappableRoles =
    [
        HrManager,
        new("hr_assistant", "HR Assistant"),
        new("office_manager", "Office manager"),
        new("system_administrator", "System Administrator"),
        new("accountant", "Assistant accountant"),
        new("english_teacher", "English Teacher")
    ];

    public static OnboardingRoleDefinition? Find(string? key)
    {
        return MappableRoles.FirstOrDefault(r => r.Key == key);
    }
}
