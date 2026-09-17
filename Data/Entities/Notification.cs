namespace DsacReporting.Api.Data.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty; // nudge, escalation
    public string Channel { get; set; } = "email";
    public DateTimeOffset SentAt { get; set; }
}
