namespace DsacReporting.Api.Data.Entities;

public class EntityKpi
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid? FormSchemaId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? FiveYearTarget { get; set; }
    public string FormValues { get; set; } = "{}";
    public string Status { get; set; } = "draft"; // draft, sent, received
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
}
