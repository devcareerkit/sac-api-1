using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    public DashboardService(AppDbContext db) => _db = db;

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync()
    {
        var currentCycle = await _db.ReportingCycles
            .OrderByDescending(c => c.DueDate)
            .FirstOrDefaultAsync();

        var entities = await _db.Entities.OrderBy(e => e.Name).ToListAsync();

        var submissionsByEntity = currentCycle is null
            ? new Dictionary<Guid, string>()
            : await _db.Submissions
                .Where(s => s.CycleId == currentCycle.Id)
                .ToDictionaryAsync(s => s.EntityId, s => s.Status);

        var summary = new PortfolioSummaryDto { Total = entities.Count };

        foreach (var entity in entities)
        {
            var status = submissionsByEntity.GetValueOrDefault(entity.Id, "not_started");

            switch (status)
            {
                case "in_progress": summary.InProgress++; break;
                case "submitted": summary.Submitted++; break;
                case "missed": summary.Missed++; break;
                default: summary.NotStarted++; break;
            }

            summary.Entities.Add(new EntityStatusSummaryDto
            {
                EntityId = entity.Id,
                EntityName = entity.Name,
                Status = status,
            });
        }

        return summary;
    }
}
