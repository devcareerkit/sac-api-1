namespace DsacReporting.Api.DTOs;

// Matches the frontend's KpiSubmissionForm fields directly, so the UI doesn't
// need to know KPI target ids or the current cycle id.
public class KpiReportDto
{
    public int JobsCreated { get; set; }
    public decimal BudgetSpent { get; set; }
    public string? VarianceNotes { get; set; }
}
