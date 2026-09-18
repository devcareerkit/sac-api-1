using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class AppIndicatorService : IAppIndicatorService
{
    private readonly AppDbContext _db;
    public AppIndicatorService(AppDbContext db) => _db = db;

    public async Task<PagedIndicatorsDto> ListForEntityAsync(Guid entityId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.AppIndicators
            .Where(i => i.EntityId == entityId && i.IsApproved)
            .OrderBy(i => i.Name);

        var totalCount = await query.CountAsync();

        var indicators = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var indicatorIds = indicators.Select(i => i.Id).ToList();
        var quarters = await _db.AppIndicatorQuarters
            .Where(q => indicatorIds.Contains(q.AppIndicatorId))
            .ToListAsync();

        var items = indicators.Select(i => MapDetail(i, quarters.Where(q => q.AppIndicatorId == i.Id))).ToList();

        return new PagedIndicatorsDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<AppIndicatorDetailDto?> GetAsync(Guid indicatorId)
    {
        var indicator = await _db.AppIndicators.FirstOrDefaultAsync(i => i.Id == indicatorId);
        if (indicator is null) return null;

        var quarters = await _db.AppIndicatorQuarters
            .Where(q => q.AppIndicatorId == indicatorId)
            .ToListAsync();

        return MapDetail(indicator, quarters);
    }

    public async Task<AppIndicatorQuarterResponseDto> SubmitProofAsync(
        Guid indicatorId, short quarter, string proofFileUrl, string? notes, Guid completedBy)
    {
        var quarterRow = await _db.AppIndicatorQuarters
            .FirstOrDefaultAsync(q => q.AppIndicatorId == indicatorId && q.Quarter == quarter)
            ?? throw new InvalidOperationException("Quarter not found for this indicator.");

        quarterRow.Status = "completed";
        quarterRow.ProofFileUrl = proofFileUrl;
        quarterRow.ProofNotes = notes;
        quarterRow.CompletedBy = completedBy;
        quarterRow.CompletedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await RollUpIndicatorStatusAsync(indicatorId);

        return new AppIndicatorQuarterResponseDto
        {
            Id = quarterRow.Id,
            Quarter = quarterRow.Quarter,
            QuarterTarget = quarterRow.QuarterTarget,
            Status = quarterRow.Status,
            ProofFileUrl = quarterRow.ProofFileUrl,
            ProofNotes = quarterRow.ProofNotes,
            CompletedAt = quarterRow.CompletedAt,
        };
    }

    public async Task<List<IndicatorPortfolioSummaryDto>> GetPortfolioSummaryAsync()
    {
        var entities = await _db.Entities.OrderBy(e => e.Name).ToListAsync();
        var indicators = await _db.AppIndicators.Where(i => i.IsApproved).ToListAsync();

        var summary = new List<IndicatorPortfolioSummaryDto>();

        foreach (var entity in entities)
        {
            var entityIndicators = indicators.Where(i => i.EntityId == entity.Id).ToList();
            var total = entityIndicators.Count;
            var completed = entityIndicators.Count(i => i.Status == "completed");

            var percentComplete = total == 0 ? 0 : Math.Round((decimal)completed / total * 100, 1);

            summary.Add(new IndicatorPortfolioSummaryDto
            {
                EntityId = entity.Id,
                EntityName = entity.Name,
                PercentComplete = percentComplete,
                PercentRemaining = 100 - percentComplete,
                TotalIndicators = total,
            });
        }

        return summary;
    }

    private async Task RollUpIndicatorStatusAsync(Guid indicatorId)
    {
        var indicator = await _db.AppIndicators.FirstOrDefaultAsync(i => i.Id == indicatorId);
        if (indicator is null) return;

        var quarters = await _db.AppIndicatorQuarters
            .Where(q => q.AppIndicatorId == indicatorId)
            .ToListAsync();

        var completedCount = quarters.Count(q => q.Status == "completed");

        indicator.Status = completedCount switch
        {
            0 => "not_started",
            var c when c == quarters.Count => "completed",
            _ => "in_progress",
        };

        await _db.SaveChangesAsync();
    }

    private static AppIndicatorDetailDto MapDetail(
        Data.Entities.AppIndicator indicator, IEnumerable<Data.Entities.AppIndicatorQuarter> quarters)
    {
        return new AppIndicatorDetailDto
        {
            Id = indicator.Id,
            Name = indicator.Name,
            AnnualTarget = indicator.AnnualTarget,
            Unit = indicator.Unit,
            Status = indicator.Status,
            Quarters = quarters
                .OrderBy(q => q.Quarter)
                .Select(q => new AppIndicatorQuarterResponseDto
                {
                    Id = q.Id,
                    Quarter = q.Quarter,
                    QuarterTarget = q.QuarterTarget,
                    Status = q.Status,
                    ProofFileUrl = q.ProofFileUrl,
                    ProofNotes = q.ProofNotes,
                    CompletedAt = q.CompletedAt,
                })
                .ToList(),
        };
    }
}
