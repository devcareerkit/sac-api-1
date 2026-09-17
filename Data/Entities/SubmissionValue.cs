namespace DsacReporting.Api.Data.Entities;

public class SubmissionValue
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid KpiTargetId { get; set; }
    public decimal? ActualValue { get; set; }
    public string? Notes { get; set; }
}
