namespace DsacReporting.Api.Data.Entities;

public class AppIndicator
{
    public Guid Id { get; set; }
    public Guid AppSubmissionId { get; set; }
    public Guid EntityId { get; set; }
    public Guid? EntityKpiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? AnnualTarget { get; set; }
    public string? Unit { get; set; }
    public string? MatchConfidence { get; set; } // matched, unmatched, manual
    public bool IsApproved { get; set; }
    public string Status { get; set; } = "not_started"; // not_started, in_progress, completed
    public DateTimeOffset CreatedAt { get; set; }
}
