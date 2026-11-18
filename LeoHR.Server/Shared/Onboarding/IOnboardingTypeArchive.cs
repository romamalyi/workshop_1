namespace LeoHR.Server.Shared.Onboarding;

public interface IOnboardingTypeArchive
{
    Task<List<AffectedTypeDto>> GetTypesLeftEmptyBy(int templateId);

    Task ArchiveTypesLeftEmptyBy(int templateId);
}
