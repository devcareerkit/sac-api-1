namespace DsacReporting.Api.DTOs;

public class SubmissionSummaryStatsDto
{
    public int InProgress { get; set; }
    public int NotStarted { get; set; }
    public int Completed { get; set; }
    public int? DaysUntilDeadline { get; set; }
    public decimal Beneficiaries { get; set; }
    public decimal JobsCreated { get; set; }
}
