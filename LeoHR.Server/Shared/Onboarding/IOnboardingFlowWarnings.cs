namespace LeoHR.Server.Shared.Onboarding;

/// Implemented by Features/OnboardingFlows/OnboardingFlowWarningsService.
public interface IOnboardingFlowWarnings
{
    /// Running flows with at least one warning: the same flows the Flows page and the flow page banner flag.
    Task<int> CountFlowsWithWarningsAsync();
}
