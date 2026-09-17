namespace DsacReporting.Api.Data.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? AssignedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "open"; // open, done
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
