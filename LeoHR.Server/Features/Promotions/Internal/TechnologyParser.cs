using LeoHR.Server.Models.Entities.LeoHr;

namespace LeoHR.Server.Features.Promotions.Internal;

public static class TechnologyParser
{
    private static readonly (string Keyword, Technology Technology)[] keywordsByPriority =
    [
        ("flutter", Technology.Flutter),
        (".net", Technology.Dotnet),
        ("dotnet", Technology.Dotnet),
        ("c#", Technology.Dotnet),
        ("ruby", Technology.Ruby),
        ("php", Technology.Php),
        ("android", Technology.Android),
        ("ios", Technology.Ios),
        ("web ui", Technology.WebUi),
        ("webui", Technology.WebUi),
        ("front", Technology.WebUi),
    ];

    public static Technology? TryParse(string? profile)
    {
        if (string.IsNullOrWhiteSpace(profile)) return null;

        foreach ((string keyword, Technology technology) in keywordsByPriority)
        {
            if (profile.Contains(keyword, StringComparison.OrdinalIgnoreCase)) return technology;
        }

        return null;
    }
}
