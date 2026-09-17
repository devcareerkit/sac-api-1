namespace DsacReporting.Api.Data.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? EntityId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "entity_officer", "dsac_me", "dsac_exec"
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
