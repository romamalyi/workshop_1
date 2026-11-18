namespace LeoHR.Server.Features.Promotions;

public sealed class PromotionsOptions
{
    public const string SectionName = "Promotions";

    // A run above this removes nothing: that many leavers at once more likely means bad Employee API data.
    public int MaxLeaversPerCleanup { get; init; } = 10;
}
