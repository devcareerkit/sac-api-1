namespace DsacReporting.Api.Data.Entities;

public class DocumentRecord
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
