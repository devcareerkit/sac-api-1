namespace DsacReporting.Api.Data.Entities;

public class AppSubmission
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public string Status { get; set; } = "pending_review"; // pending_review, ai_failed, ai_processed, approved, rejected
    public string? AiSummary { get; set; }
    public DateTimeOffset? AiProcessedAt { get; set; }
    public Guid? AiProcessedBy { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
}
