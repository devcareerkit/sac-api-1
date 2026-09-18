namespace DsacReporting.Api.Data.Entities;

public class AppIndicatorQuarter
{
    public Guid Id { get; set; }
    public Guid AppIndicatorId { get; set; }
    public short Quarter { get; set; } // 1-4
    public decimal? QuarterTarget { get; set; }
    public string Status { get; set; } = "not_started"; // not_started, completed
    public string? ProofFileUrl { get; set; }
    public string? ProofNotes { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
