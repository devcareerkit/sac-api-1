using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class AlertsService : IAlertsService
{
    private readonly AppDbContext _db;
    public AlertsService(AppDbContext db) => _db = db;

    public async Task<List<AlertDto>> GetActiveAlertsAsync()
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync();
        if (currentCycle is null)
        {
            return new List<AlertDto>();
        }

        var riskScores = await _db.RiskScores
            .Where(r => r.CycleId == currentCycle.Id && r.Score >= 40)
            .OrderByDescending(r => r.Score)
            .ToListAsync();

        if (riskScores.Count == 0)
        {
            return new List<AlertDto>();
        }

        var entityIds = riskScores.Select(r => r.EntityId).ToList();
        var entities = await _db.Entities.Where(e => entityIds.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name);

        return riskScores.Select(r => new AlertDto
        {
            Id = r.Id,
            Entity = entities.GetValueOrDefault(r.EntityId, "Unknown entity"),
            Type = r.Score >= 70 ? "High Risk Escalation" : "Risk Watch",
            Message = string.IsNullOrWhiteSpace(r.Reason)
                ? $"Risk score is {r.Score}/100 for the current reporting cycle."
                : r.Reason,
            Severity = r.Score >= 70 ? "high" : "medium",
        }).ToList();
    }
}
