namespace LeoHR.Server.Models.Entities.Base;

public abstract class BaseAuditableEntity
{
    public string? CreatedById { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
}
