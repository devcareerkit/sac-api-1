namespace DsacReporting.Api.DTOs;

public class KpiValueDto
{
    public Guid KpiTargetId { get; set; }
    public decimal? ActualValue { get; set; }
    public string? Notes { get; set; }
}
