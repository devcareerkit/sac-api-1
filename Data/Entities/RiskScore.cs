namespace DsacReporting.Api.Data.Entities;

public class RiskScore
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public decimal Score { get; set; } // 0-100
    public string? Reason { get; set; }
    public DateTimeOffset ComputedAt { get; set; }
}
