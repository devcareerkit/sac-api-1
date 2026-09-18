using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class EntityPortfolioService : IEntityPortfolioService
{
    private readonly AppDbContext _db;
    public EntityPortfolioService(AppDbContext db) => _db = db;

    public async Task<List<EntityPortfolioDto>> ListPortfolioAsync()
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync();
        var entities = await _db.Entities.OrderBy(e => e.Name).ToListAsync();

        var statusByEntity = currentCycle is null
            ? new Dictionary<Guid, string>()
            : await _db.Submissions.Where(s => s.CycleId == currentCycle.Id)
                .ToDictionaryAsync(s => s.EntityId, s => s.Status);

        var riskByEntity = currentCycle is null
            ? new Dictionary<Guid, decimal>()
            : await _db.RiskScores.Where(r => r.CycleId == currentCycle.Id)
                .ToDictionaryAsync(r => r.EntityId, r => r.Score);

        return entities.Select(e => BuildPortfolioDto(e, statusByEntity, riskByEntity)).ToList();
    }

    public async Task<EntityDetailDto?> GetEntityDetailAsync(Guid entityId)
    {
        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == entityId);
        return entity is null ? null : await BuildDetailDtoAsync(entity);
    }

    public async Task<EntityDetailDto?> GetEntityDetailBySlugAsync(string slug)
    {
        var entities = await _db.Entities.ToListAsync();
        var entity = entities.FirstOrDefault(e => SlugHelper.ToSlug(e.Name) == slug);
        return entity is null ? null : await BuildDetailDtoAsync(entity);
    }

    private async Task<EntityDetailDto> BuildDetailDtoAsync(Data.Entities.Entity entity)
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync();

        var status = "not_started";
        decimal risk = 0;

        if (currentCycle is not null)
        {
            var submission = await _db.Submissions
                .FirstOrDefaultAsync(s => s.EntityId == entity.Id && s.CycleId == currentCycle.Id);
            status = submission?.Status ?? "not_started";

            var riskScore = await _db.RiskScores
                .FirstOrDefaultAsync(r => r.EntityId == entity.Id && r.CycleId == currentCycle.Id);
            risk = riskScore?.Score ?? 0;
        }

        var dto = new EntityDetailDto
        {
            Id = entity.Id,
            Slug = SlugHelper.ToSlug(entity.Name),
            Name = entity.Name,
            Type = entity.Type,
            CreatedAt = entity.CreatedAt,
            Status = MapStatus(status),
            Risk = MapRisk(risk),
            Score = risk,
        };

        var kpiTargets = currentCycle is null
            ? new List<Data.Entities.KpiTarget>()
            : await _db.KpiTargets.Where(k => k.EntityId == entity.Id && k.CycleId == currentCycle.Id).ToListAsync();

        var submissionForCycle = currentCycle is null
            ? null
            : await _db.Submissions.FirstOrDefaultAsync(s => s.EntityId == entity.Id && s.CycleId == currentCycle.Id);

        var values = submissionForCycle is null
            ? new List<Data.Entities.SubmissionValue>()
            : await _db.SubmissionValues.Where(v => v.SubmissionId == submissionForCycle.Id).ToListAsync();

        dto.Kpis = kpiTargets.Select(k =>
        {
            var value = values.FirstOrDefault(v => v.KpiTargetId == k.Id);
            var onTrack = value?.ActualValue.HasValue == true && k.TargetValue.HasValue
                && value.ActualValue!.Value >= k.TargetValue.Value;

            return new KpiRollupDto
            {
                Id = k.Id,
                Name = k.KpiName,
                Target = k.TargetValue.HasValue ? $"{k.TargetValue.Value} {k.Unit}".Trim() : null,
                Actual = value?.ActualValue.HasValue == true ? $"{value.ActualValue.Value} {k.Unit}".Trim() : null,
                OnTrack = onTrack,
            };
        }).ToList();

        var documents = await _db.DocumentRecords
            .Where(d => d.EntityId == entity.Id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        dto.Documents = documents.Select(d => new DocumentSummaryDto
        {
            Id = d.Id,
            Name = SlugHelper.ExtractFileName(d.FileUrl),
            UploadedAt = d.UploadedAt,
        }).ToList();

        return dto;
    }

    private EntityPortfolioDto BuildPortfolioDto(
        Data.Entities.Entity entity,
        Dictionary<Guid, string> statusByEntity,
        Dictionary<Guid, decimal> riskByEntity)
    {
        var status = statusByEntity.GetValueOrDefault(entity.Id, "not_started");
        var risk = riskByEntity.GetValueOrDefault(entity.Id, 0);

        return new EntityPortfolioDto
        {
            Id = entity.Id,
            Slug = SlugHelper.ToSlug(entity.Name),
            Name = entity.Name,
            Type = entity.Type,
            CreatedAt = entity.CreatedAt,
            Status = MapStatus(status),
            Risk = MapRisk(risk),
            Score = risk,
        };
    }

    private static string MapStatus(string dbStatus) => dbStatus switch
    {
        "submitted" => "Submitted",
        "in_progress" => "In Progress",
        "missed" => "Not Started",
        _ => "Not Started",
    };

    private static string MapRisk(decimal score) => score switch
    {
        >= 70 => "High",
        >= 40 => "Watch",
        _ => "Low",
    };

}
