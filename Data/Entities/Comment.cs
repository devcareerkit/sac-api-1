namespace DsacReporting.Api.Data.Entities;

public class Comment
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
