namespace DsacReporting.Api.DTOs;

public class EntityTrendDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public List<CyclePointDto> Cycles { get; set; } = new();
}

public class CyclePointDto
{
    public Guid CycleId { get; set; }
    public string CycleLabel { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty; // not_started, in_progress, submitted, missed
}

public class PortfolioTrendDto
{
    // One point per cycle: how many entities were on-time (submitted) vs
    // missed, across the whole portfolio - the year-on-year / period-over-
    // period comparison view.
    public List<CycleAggregateDto> Cycles { get; set; } = new();
}

public class CycleAggregateDto
{
    public Guid CycleId { get; set; }
    public string CycleLabel { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int SubmittedCount { get; set; }
    public int MissedCount { get; set; }
    public int OtherCount { get; set; }
}
