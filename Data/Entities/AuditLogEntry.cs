namespace DsacReporting.Api.Data.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public Guid? TargetId { get; set; }
    public string? Metadata { get; set; } // JSON stored as text
    public DateTimeOffset CreatedAt { get; set; }
}
