namespace DsacReporting.Api.DTOs;

public class AppIndicatorDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? AnnualTarget { get; set; }
    public string? Unit { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<AppIndicatorQuarterResponseDto> Quarters { get; set; } = new();
}

public class AppIndicatorQuarterResponseDto
{
    public Guid Id { get; set; }
    public short Quarter { get; set; }
    public decimal? QuarterTarget { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProofFileUrl { get; set; }
    public string? ProofNotes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class PagedIndicatorsDto
{
    public List<AppIndicatorDetailDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class IndicatorPortfolioSummaryDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public decimal PercentComplete { get; set; }
    public decimal PercentRemaining { get; set; }
    public int TotalIndicators { get; set; }
}
