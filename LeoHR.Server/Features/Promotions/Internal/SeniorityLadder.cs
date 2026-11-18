using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class SeniorityLadder
{
    private static readonly int subLevelCount = Enum.GetValues<SubLevel>().Length;

    public static string? FindNextLevel(IEnumerable<PromotionLevel> technologyLevels, string role)
    {
        return LevelsAbove(technologyLevels, role).FirstOrDefault();
    }

    public static List<string> LevelsAbove(IEnumerable<PromotionLevel> technologyLevels, string role)
    {
        int currentRank = Rank(role);
        if (currentRank < 0) return [];

        return [.. technologyLevels
            .Where(level => Rank(level.Seniority, level.SubLevel) > currentRank)
            .OrderBy(level => Rank(level.Seniority, level.SubLevel))
            .Select(level => Name(level.Seniority, level.SubLevel))];
    }

    public static string? Higher(string? firstRole, string? secondRole)
    {
        string? higherRole = Rank(firstRole) >= Rank(secondRole) ? firstRole : secondRole;
        (Seniority? seniority, SubLevel subLevel) = SeniorityParser.TryParse(higherRole ?? string.Empty);
        return seniority is null ? null : Name(seniority.Value, subLevel);
    }

    public static int Rank(string? role)
    {
        (Seniority? seniority, SubLevel subLevel) = SeniorityParser.TryParse(role ?? string.Empty);
        return seniority is null ? -1 : Rank(seniority.Value, subLevel);
    }

    public static int Rank(Seniority seniority, SubLevel subLevel)
    {
        return ((int)seniority * subLevelCount) + (int)subLevel;
    }

    private static string Name(Seniority seniority, SubLevel subLevel)
    {
        return subLevel == SubLevel.None ? seniority.ToString() : $"{subLevel} {seniority}";
    }
}
