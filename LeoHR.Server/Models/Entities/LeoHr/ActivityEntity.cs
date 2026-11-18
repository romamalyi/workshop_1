namespace LeoHR.Server.Models.Entities.LeoHr;

public class ActivityEntity
{
    public int Id { get; set; }
    public ActivityCategory Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public ActivityPeriod WithinPeriod { get; set; }
    public Technology? Technology { get; set; }
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
