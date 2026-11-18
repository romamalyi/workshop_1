namespace LeoHR.Server.Models.Entities.LeoHr;

public class AppSettingsEntity
{
    public const string DefaultPromotionHolder = "sh";

    public string UserId { get; set; } = string.Empty;

    public string PromotionDefaultHolder { get; set; } = DefaultPromotionHolder;

    public bool ShowManager { get; set; } = false;
    public bool ShowMentor { get; set; } = false;
    public bool ShowEmployment { get; set; } = false;
}
