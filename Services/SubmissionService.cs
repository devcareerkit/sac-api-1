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

    public async Task<SubmissionSummaryStatsDto> GetSummaryAsync(Guid? entityId)
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync();

        var summary = new SubmissionSummaryStatsDto
        {
            DaysUntilDeadline = currentCycle is null
                ? null
                : (int)Math.Ceiling((currentCycle.DueDate.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays),
        };

        if (currentCycle is null)
        {
            return summary;
        }

        var submissionsQuery = _db.Submissions.Where(s => s.CycleId == currentCycle.Id);
        if (entityId.HasValue)
        {
            submissionsQuery = submissionsQuery.Where(s => s.EntityId == entityId.Value);
        }

        var submissions = await submissionsQuery.ToListAsync();
        summary.Completed = submissions.Count(s => s.Status == "submitted");
        summary.InProgress = submissions.Count(s => s.Status == "in_progress");
        summary.NotStarted = submissions.Count(s => s.Status == "not_started" || s.Status == "missed");

        var submissionIds = submissions.Select(s => s.Id).ToList();
        var values = submissionIds.Count == 0
            ? new List<Data.Entities.SubmissionValue>()
            : await _db.SubmissionValues.Where(v => submissionIds.Contains(v.SubmissionId)).ToListAsync();

        var kpiTargetIds = values.Select(v => v.KpiTargetId).Distinct().ToList();
        var kpiTargets = kpiTargetIds.Count == 0
            ? new List<Data.Entities.KpiTarget>()
            : await _db.KpiTargets.Where(k => kpiTargetIds.Contains(k.Id)).ToListAsync();

        summary.JobsCreated = values
            .Where(v => kpiTargets.Any(k => k.Id == v.KpiTargetId && k.KpiName == "Job creation"))
            .Sum(v => v.ActualValue ?? 0);

        summary.Beneficiaries = values
            .Where(v => kpiTargets.Any(k => k.Id == v.KpiTargetId && k.KpiName == "Beneficiaries reached"))
            .Sum(v => v.ActualValue ?? 0);

        return summary;
    }

    public async Task<Guid> SubmitKpiReportAsync(Guid entityId, KpiReportDto report, Guid submittedBy)
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No reporting cycle is configured.");

        var targets = await _db.KpiTargets
            .Where(k => k.EntityId == entityId && k.CycleId == currentCycle.Id)
            .ToListAsync();

        var jobsTarget = targets.FirstOrDefault(k => k.KpiName == "Job creation");
        var budgetTarget = targets.FirstOrDefault(k => k.KpiName == "Budget spent");

        var values = new List<KpiValueDto>();
        if (jobsTarget is not null)
        {
            values.Add(new KpiValueDto
            {
                KpiTargetId = jobsTarget.Id,
                ActualValue = report.JobsCreated,
                Notes = report.VarianceNotes,
            });
        }

        if (budgetTarget is not null)
        {
            values.Add(new KpiValueDto
            {
                KpiTargetId = budgetTarget.Id,
                ActualValue = report.BudgetSpent,
                Notes = report.VarianceNotes,
            });
        }

        return await CreateSubmissionAsync(
            new SubmissionDto { EntityId = entityId, CycleId = currentCycle.Id, Values = values },
            submittedBy);
    }
}
