namespace DsacReporting.Api.Data.Entities;

public class RiskScore
{
    public int Id { get; set; }
    public int EntityId { get; set; }
    public decimal Score { get; set; }
    public DateTime CalculatedAt { get; set; }
}
