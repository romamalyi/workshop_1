using LeoHR.Server.Shared.Models.Filter;

namespace LeoHR.Server.Features.OnboardingFlows.Dto;

public enum OnboardingTaskScope
{
    All,
    Mine,
}

public enum OnboardingTaskListStatus
{
    Open,
    Overdue,
    Done,
    All,
}

/// <remarks><c>Today</c> is the caller's local date, so Overdue and Due today match what the page shows.</remarks>
public record OnboardingTaskFilterModel(
    OnboardingTaskScope Scope = OnboardingTaskScope.All,
    OnboardingTaskListStatus Status = OnboardingTaskListStatus.Open,
    DateOnly? Today = null
) : BaseFilterModel;
