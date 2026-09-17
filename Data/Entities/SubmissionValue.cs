namespace DsacReporting.Api.Data.Entities;

public class SubmissionValue
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
