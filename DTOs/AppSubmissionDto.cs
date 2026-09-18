namespace DsacReporting.Api.DTOs;

public class AppSubmissionResponseDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AiSummary { get; set; }
    public string? RejectionReason { get; set; }
    public List<AppIndicatorResponseDto> Indicators { get; set; } = new();
}

public class AppIndicatorResponseDto
{
    public Guid Id { get; set; }
    public Guid? EntityKpiId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? AnnualTarget { get; set; }
    public string? Unit { get; set; }
    public string? MatchConfidence { get; set; }
    public bool IsApproved { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RejectAppSubmissionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class ApproveAppSubmissionDto
{
    // Indicator ids from the analyze preview that Sipho wants to KEEP.
    // Any app_indicators row for this submission not in this list is discarded.
    public List<Guid> KeepIndicatorIds { get; set; } = new();
}
