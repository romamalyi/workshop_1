namespace LeoHR.Server.Shared.Onboarding;

public interface IOnboardingCandidates
{
    Task<List<ResponsibleCandidateDto>> GetResponsibleCandidates();

    Task<List<EmployeeCandidateDto>> GetAssigneeCandidates(string? roleKey);

    Task<List<EmployeeCandidateDto>> GetPersonFieldCandidates(string? restrictionKey);
}
