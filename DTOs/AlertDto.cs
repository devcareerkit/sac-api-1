namespace DsacReporting.Api.DTOs;

public class AlertDto
{
    public Guid Id { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "low"; // "high" | "medium" | "low"
}
