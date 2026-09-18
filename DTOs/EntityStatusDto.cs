namespace DsacReporting.Api.DTOs;

public class EntityStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<SubmissionSummaryDto> Submissions { get; set; } = new();
}
