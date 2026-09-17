namespace DsacReporting.Api.Data.Entities;

public class ReportingCycle
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
