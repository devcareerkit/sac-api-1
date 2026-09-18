namespace DsacReporting.Api.DTOs;

public class SubmissionSummaryDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid CycleId { get; set; }
    public string CycleLabel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; set; }
}
