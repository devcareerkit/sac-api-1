using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class SubmissionService : ISubmissionService
{
    private readonly AppDbContext _db;
    public SubmissionService(AppDbContext db) => _db = db;

    public async Task<Guid> CreateSubmissionAsync(SubmissionDto dto, Guid submittedBy)
    {
        var submission = await _db.Submissions
            .FirstOrDefaultAsync(s => s.EntityId == dto.EntityId && s.CycleId == dto.CycleId);

        if (submission is null)
        {
            submission = new Data.Entities.Submission
            {
                Id = Guid.NewGuid(),
                EntityId = dto.EntityId,
                CycleId = dto.CycleId,
            };
            _db.Submissions.Add(submission);
        }

        submission.SubmittedBy = submittedBy;
        submission.Status = "submitted";
        submission.SubmittedAt = DateTimeOffset.UtcNow;

        // Save the submission first: SubmissionValue.SubmissionId is a plain Guid, not an EF
        // navigation property, so EF's dependency graph doesn't know to insert Submission
        // before SubmissionValue — without this, a new submission's FK insert fails.
        await _db.SaveChangesAsync();

        foreach (var value in dto.Values)
        {
            var existing = await _db.SubmissionValues.FirstOrDefaultAsync(v =>
                v.SubmissionId == submission.Id && v.KpiTargetId == value.KpiTargetId);

            if (existing is null)
            {
                _db.SubmissionValues.Add(new Data.Entities.SubmissionValue
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    KpiTargetId = value.KpiTargetId,
                    ActualValue = value.ActualValue,
                    Notes = value.Notes,
                });
            }
            else
            {
                existing.ActualValue = value.ActualValue;
                existing.Notes = value.Notes;
            }
        }

        await _db.SaveChangesAsync();
        return submission.Id;
    }

    public async Task<List<SubmissionSummaryDto>> ListSubmissionsAsync(Guid? entityId)
    {
        var query = _db.Submissions
            .Join(_db.Entities, s => s.EntityId, e => e.Id, (s, e) => new { Submission = s, Entity = e })
            .Join(_db.ReportingCycles, x => x.Submission.CycleId, c => c.Id, (x, c) => new { x.Submission, x.Entity, Cycle = c })
            .AsQueryable();

        if (entityId.HasValue)
        {
            query = query.Where(x => x.Submission.EntityId == entityId.Value);
        }

        return await query
            .OrderByDescending(x => x.Submission.SubmittedAt)
            .Select(x => new SubmissionSummaryDto
            {
                Id = x.Submission.Id,
                EntityId = x.Submission.EntityId,
                EntityName = x.Entity.Name,
                CycleId = x.Submission.CycleId,
                CycleLabel = x.Cycle.Label,
                Status = x.Submission.Status,
                SubmittedAt = x.Submission.SubmittedAt,
            })
            .ToListAsync();
    }
}
