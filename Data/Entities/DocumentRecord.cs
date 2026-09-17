namespace DsacReporting.Api.Data.Entities;

public class DocumentRecord
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid EntityId { get; set; }
    public string DocType { get; set; } = string.Empty;
    // strategic_plan, app, operational_plan, annual_report, quarterly_report, financials
    public string FileUrl { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid? KpiTagId { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
