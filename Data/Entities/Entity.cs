namespace DsacReporting.Api.Data.Entities;

public class Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "public_entity" or "npo"
    public DateTimeOffset CreatedAt { get; set; }
}
