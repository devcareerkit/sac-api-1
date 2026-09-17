namespace DsacReporting.Api.Data.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid CycleId { get; set; }
    public Guid? SubmittedBy { get; set; }
    public string Status { get; set; } = "not_started"; // not_started, in_progress, submitted, missed
    public DateTimeOffset? SubmittedAt { get; set; }
}
