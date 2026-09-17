namespace DsacReporting.Api.Data.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
