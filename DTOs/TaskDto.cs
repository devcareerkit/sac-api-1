namespace DsacReporting.Api.DTOs;

public class CreateTaskDto
{
    public Guid? SubmissionId { get; set; }
    public Guid? AssignedTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
}

public class TaskResponseDto
{
    public Guid Id { get; set; }
    public Guid? SubmissionId { get; set; }
    public Guid? AssignedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
