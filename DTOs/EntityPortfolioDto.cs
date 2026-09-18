namespace DsacReporting.Api.DTOs;

public class EntityPortfolioDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = "Not Started"; // "Submitted" | "In Progress" | "Not Started"
    public string Risk { get; set; } = "Watch"; // "Low" | "Watch" | "High"
    public decimal Score { get; set; }
}

public class EntityDetailDto : EntityPortfolioDto
{
    public List<KpiRollupDto> Kpis { get; set; } = new();
    public List<DocumentSummaryDto> Documents { get; set; } = new();
}

public class KpiRollupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? Actual { get; set; }
    public bool OnTrack { get; set; }
}

public class DocumentSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}
