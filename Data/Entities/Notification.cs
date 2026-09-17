namespace DsacReporting.Api.Data.Entities;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Read { get; set; }
}
