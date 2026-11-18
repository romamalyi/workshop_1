namespace LeoHR.Server.Models.Entities.LeoHr;

public class EmployeeExtensionEntity
{
    public string EmployeeId { get; set; } = null!;
    public DateTime? LevelStartDate { get; set; }
    public string? PdpUrl { get; set; }
}
