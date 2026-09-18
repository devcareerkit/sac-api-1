namespace DsacReporting.Api.DTOs;

public class CreateEntityKpiDto
{
    public string KpiName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? FiveYearTarget { get; set; }
    public Guid? FormSchemaId { get; set; }
    public string FormValues { get; set; } = "{}";
}

public class SubmitEntityKpisDto
{
    public List<CreateEntityKpiDto> Kpis { get; set; } = new();
}

public class EntityKpiResponseDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string KpiName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal? FiveYearTarget { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
}
