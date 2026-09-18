using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class EntityKpiService : IEntityKpiService
{
    private readonly AppDbContext _db;
    public EntityKpiService(AppDbContext db) => _db = db;

    public async Task<List<EntityKpiResponseDto>> ListForEntityAsync(Guid entityId)
    {
        return await _db.EntityKpis
            .Where(k => k.EntityId == entityId)
            .OrderBy(k => k.KpiName)
            .Select(k => new EntityKpiResponseDto
            {
                Id = k.Id,
                EntityId = k.EntityId,
                KpiName = k.KpiName,
                Unit = k.Unit,
                FiveYearTarget = k.FiveYearTarget,
                Status = k.Status,
                CreatedAt = k.CreatedAt,
                SentAt = k.SentAt,
                ReceivedAt = k.ReceivedAt,
            })
            .ToListAsync();
    }

    public async Task<List<EntityKpiResponseDto>> SubmitAsync(Guid entityId, SubmitEntityKpisDto dto, Guid createdBy)
    {
        var created = new List<Data.Entities.EntityKpi>();

        foreach (var kpi in dto.Kpis)
        {
            var existing = await _db.EntityKpis
                .FirstOrDefaultAsync(k => k.EntityId == entityId && k.KpiName == kpi.KpiName);

            if (existing is not null)
            {
                existing.Unit = kpi.Unit;
                existing.FiveYearTarget = kpi.FiveYearTarget;
                existing.FormSchemaId = kpi.FormSchemaId;
                existing.FormValues = kpi.FormValues;
                created.Add(existing);
                continue;
            }

            var entityKpi = new Data.Entities.EntityKpi
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                FormSchemaId = kpi.FormSchemaId,
                KpiName = kpi.KpiName,
                Unit = kpi.Unit,
                FiveYearTarget = kpi.FiveYearTarget,
                FormValues = kpi.FormValues,
                Status = "draft",
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            _db.EntityKpis.Add(entityKpi);
            created.Add(entityKpi);
        }

        await _db.SaveChangesAsync();

        return created.Select(k => new EntityKpiResponseDto
        {
            Id = k.Id,
            EntityId = k.EntityId,
            KpiName = k.KpiName,
            Unit = k.Unit,
            FiveYearTarget = k.FiveYearTarget,
            Status = k.Status,
            CreatedAt = k.CreatedAt,
            SentAt = k.SentAt,
            ReceivedAt = k.ReceivedAt,
        }).ToList();
    }

    public async Task<int> SendAsync(Guid entityId)
    {
        var drafts = await _db.EntityKpis
            .Where(k => k.EntityId == entityId && k.Status == "draft")
            .ToListAsync();

        foreach (var kpi in drafts)
        {
            kpi.Status = "sent";
            kpi.SentAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return drafts.Count;
    }
}
