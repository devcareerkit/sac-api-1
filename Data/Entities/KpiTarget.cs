namespace DsacReporting.Api.Data.Entities;

public class KpiTarget
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public decimal? TargetValue { get; set; }
    public string? Unit { get; set; }
}
