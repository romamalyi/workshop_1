using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class SeniorityParser
{
    public static (Seniority? seniority, SubLevel subLevel) TryParse(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return (null, SubLevel.None);

        string[] parts = role.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Seniority? seniority = null;
        SubLevel subLevel = SubLevel.None;

        foreach (string part in parts)
        {
            if (seniority is null && Enum.TryParse<Seniority>(part, ignoreCase: true, out Seniority s))
                seniority = s;
            else if (Enum.TryParse<SubLevel>(part, ignoreCase: true, out SubLevel sl) && sl != SubLevel.None)
                subLevel = sl;
        }

        return (seniority, subLevel);
    }
}
