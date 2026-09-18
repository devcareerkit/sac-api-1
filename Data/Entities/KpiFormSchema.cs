namespace DsacReporting.Api.Data.Entities;

public class KpiFormSchema
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = "{}";
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
