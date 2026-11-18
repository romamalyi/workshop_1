namespace LeoHR.Server.Features.EmployeeActivities.Internal;

// The name is stored in ApprovedByName and shown in the UI; it also marks the owner of a record, so it must stay stable.
public static class IntegrationClients
{
    private static readonly Dictionary<string, string> names = new()
    {
        ["leobit.rnd-hub.web"] = "Leobit Academy",
        ["leobit.academy.service"] = "Leobit Academy",
    };

    public static string NameFor(string clientId)
    {
        return names.GetValueOrDefault(clientId, clientId);
    }
}
