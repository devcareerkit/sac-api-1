namespace DsacReporting.Api.DTOs;

public class PortfolioSummaryDto
{
    public int Total { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int Missed { get; set; }
    public List<EntityStatusSummaryDto> Entities { get; set; } = new();
}

public class EntityStatusSummaryDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
