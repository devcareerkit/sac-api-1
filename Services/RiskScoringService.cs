using DsacReporting.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class RiskScoringService : IRiskScoringService
{
    private readonly AppDbContext _db;
    public RiskScoringService(AppDbContext db) => _db = db;

    public async Task<List<RiskScoreResult>> ComputeAllAsync()
    {
        var currentCycle = await _db.ReportingCycles.OrderByDescending(c => c.DueDate).FirstOrDefaultAsync();
        var entities = await _db.Entities.ToListAsync();

        if (currentCycle is null)
        {
            return entities.Select(e => new RiskScoreResult(e.Id, 0, null)).ToList();
        }

        var submissions = await _db.Submissions
            .Where(s => s.CycleId == currentCycle.Id)
            .ToDictionaryAsync(s => s.EntityId, s => s.Status);

        var appSubmissions = await _db.AppSubmissions.ToListAsync();
        var entityKpis = await _db.EntityKpis.ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysUntilDue = currentCycle.DueDate.DayNumber - today.DayNumber;

        var results = new List<RiskScoreResult>();

        foreach (var entity in entities)
        {
            decimal score = 0;
            var reasons = new List<string>();

            var submissionStatus = submissions.GetValueOrDefault(entity.Id, "not_started");
            if (submissionStatus == "missed")
            {
                score += 40;
                reasons.Add("Reporting cycle submission missed.");
            }
            else if (daysUntilDue <= 7 && submissionStatus is "not_started" or "in_progress")
            {
                score += 20;
                reasons.Add($"Submission due in {daysUntilDue} day(s) and not yet submitted.");
            }

            var latestApp = appSubmissions
                .Where(a => a.EntityId == entity.Id)
                .OrderByDescending(a => a.UploadedAt)
                .FirstOrDefault();

            if (latestApp is not null && latestApp.Status == "rejected")
            {
                score += 30;
                reasons.Add("Latest APP submission was rejected and has not been resubmitted.");
            }

            var hasKpisSent = entityKpis.Any(k => k.EntityId == entity.Id && k.Status == "sent");
            var hasAnyAppSubmission = appSubmissions.Any(a => a.EntityId == entity.Id);
            if (hasKpisSent && !hasAnyAppSubmission)
            {
                score += 20;
                reasons.Add("KPIs sent but no APP submission has been uploaded yet.");
            }

            score = Math.Min(score, 100);
            var reason = reasons.Count > 0 ? string.Join(" ", reasons) : null;

            results.Add(new RiskScoreResult(entity.Id, score, reason));

            var existing = await _db.RiskScores
                .FirstOrDefaultAsync(r => r.EntityId == entity.Id && r.CycleId == currentCycle.Id);

            if (existing is null)
            {
                _db.RiskScores.Add(new Data.Entities.RiskScore
                {
                    Id = Guid.NewGuid(),
                    EntityId = entity.Id,
                    CycleId = currentCycle.Id,
                    Score = score,
                    Reason = reason,
                    ComputedAt = DateTimeOffset.UtcNow,
                });
            }
            else
            {
                existing.Score = score;
                existing.Reason = reason;
                existing.ComputedAt = DateTimeOffset.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return results;
    }
}
