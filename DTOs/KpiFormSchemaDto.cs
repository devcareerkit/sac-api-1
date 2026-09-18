namespace DsacReporting.Api.DTOs;

public class CreateKpiFormSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = "{}";
}

public class KpiFormSchemaResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
