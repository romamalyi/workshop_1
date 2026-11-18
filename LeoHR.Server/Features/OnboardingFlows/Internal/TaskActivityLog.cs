using System.Security.Claims;

using LeoHR.Server.Models.Entities.LeoHr;
using LeoHR.Server.Shared.Data;
using LeoHR.Server.Shared.Extensions.Authentication;

namespace LeoHR.Server.Features.OnboardingFlows.Internal;

internal sealed class TaskActivityLog(LeoHrDbContext db)
{
    private static readonly TimeSpan fieldEditSessionWindow = TimeSpan.FromMinutes(10);

    public void RecordAssigneeChange(OnboardingFlowTaskEntity task, string toAssigneeId)
    {
        db.OnboardingFlowTaskActivities.Add(new OnboardingFlowTaskActivityEntity
        {
            Task = task,
            Kind = OnboardingTaskActivityKind.AssigneeChanged,
            FromAssigneeId = task.AssigneeId,
            ToAssigneeId = toAssigneeId,
        });
    }

    // Autosave sends a typed value on every pause, so one editing session is logged once.
    public static bool ContinuesFieldEdit(
        OnboardingFlowTaskActivityEntity? latest,
        TaskFieldChange change,
        OnboardingFlowTaskFieldEntity field,
        ClaimsPrincipal user)
    {
        return change.Kind == OnboardingTaskActivityKind.FieldChanged
            && latest is { Kind: OnboardingTaskActivityKind.FieldChanged }
            && latest.FieldLabel == field.Label
            && latest.CreatedById == user.GetUserId()
            && DateTime.UtcNow - latest.CreatedAt < fieldEditSessionWindow;
    }

    public void RecordActivity(
        OnboardingFlowTaskEntity task,
        OnboardingTaskActivityKind kind,
        string? fieldLabel = null,
        string? checklistItemText = null)
    {
        db.OnboardingFlowTaskActivities.Add(new OnboardingFlowTaskActivityEntity
        {
            Task = task,
            Kind = kind,
            FieldLabel = fieldLabel,
            ChecklistItemText = checklistItemText,
        });
    }
}
