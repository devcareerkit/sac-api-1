using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class TrendService : ITrendService
{
    private readonly AppDbContext _db;
    public TrendService(AppDbContext db) => _db = db;

    public async Task<EntityTrendDto?> GetEntityTrendAsync(Guid entityId)
    {
        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == entityId);
        if (entity is null) return null;

        var cycles = await _db.ReportingCycles.OrderBy(c => c.DueDate).ToListAsync();
        var submissions = await _db.Submissions
            .Where(s => s.EntityId == entityId)
            .ToDictionaryAsync(s => s.CycleId, s => s.Status);

        return new EntityTrendDto
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            Cycles = cycles.Select(c => new CyclePointDto
            {
                CycleId = c.Id,
                CycleLabel = c.Label,
                DueDate = c.DueDate,
                Status = submissions.GetValueOrDefault(c.Id, "not_started"),
            }).ToList(),
        };
    }

    public async Task<PortfolioTrendDto> GetPortfolioTrendAsync()
    {
        var cycles = await _db.ReportingCycles.OrderBy(c => c.DueDate).ToListAsync();
        var allSubmissions = await _db.Submissions.ToListAsync();

        var result = new PortfolioTrendDto();

        foreach (var cycle in cycles)
        {
            var forCycle = allSubmissions.Where(s => s.CycleId == cycle.Id).ToList();

            result.Cycles.Add(new CycleAggregateDto
            {
                CycleId = cycle.Id,
                CycleLabel = cycle.Label,
                DueDate = cycle.DueDate,
                SubmittedCount = forCycle.Count(s => s.Status == "submitted"),
                MissedCount = forCycle.Count(s => s.Status == "missed"),
                OtherCount = forCycle.Count(s => s.Status is "not_started" or "in_progress"),
            });
        }

        return result;
    }
}
