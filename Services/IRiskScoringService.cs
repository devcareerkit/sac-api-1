namespace DsacReporting.Api.Services;

public record RiskScoreResult(Guid EntityId, decimal Score, string? Reason);

public interface IRiskScoringService
{
    // Computes (and persists to risk_scores, for the historical record) a
    // fresh risk score for every entity against the current reporting cycle.
    Task<List<RiskScoreResult>> ComputeAllAsync();
}
