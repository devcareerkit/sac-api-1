using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class AppSubmissionService : IAppSubmissionService
{
    private readonly AppDbContext _db;
    private readonly IAppExtractionService _extraction;
    private readonly IDocumentStorageService _storage;

    public AppSubmissionService(AppDbContext db, IAppExtractionService extraction, IDocumentStorageService storage)
    {
        _db = db;
        _extraction = extraction;
        _storage = storage;
    }

    public async Task<Guid> UploadAsync(Guid entityId, string fileUrl, Guid uploadedBy)
    {
        var submission = new Data.Entities.AppSubmission
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            FileUrl = fileUrl,
            UploadedBy = uploadedBy,
            UploadedAt = DateTimeOffset.UtcNow,
            Status = "pending_review",
        };

        _db.AppSubmissions.Add(submission);
        await _db.SaveChangesAsync();
        return submission.Id;
    }

    public async Task<List<AppSubmissionSummaryDto>> ListAsync(Guid? entityId, string? status)
    {
        var query = _db.AppSubmissions.AsQueryable();

        if (entityId.HasValue)
        {
            query = query.Where(s => s.EntityId == entityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var submissions = await query.OrderByDescending(s => s.UploadedAt).ToListAsync();

        var entityIds = submissions.Select(s => s.EntityId).Distinct().ToList();
        var entityNames = await _db.Entities
            .Where(e => entityIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name);

        return submissions.Select(s => new AppSubmissionSummaryDto
        {
            Id = s.Id,
            EntityId = s.EntityId,
            EntityName = entityNames.GetValueOrDefault(s.EntityId, "Unknown entity"),
            FileUrl = s.FileUrl,
            UploadedAt = s.UploadedAt,
            Status = s.Status,
        }).ToList();
    }

    public async Task<AppSubmissionResponseDto?> GetAsync(Guid submissionId)
    {
        var submission = await _db.AppSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (submission is null) return null;

        return await BuildResponseAsync(submission);
    }

    public async Task<AppSubmissionResponseDto> AnalyzeAsync(Guid submissionId, Guid processedBy, CancellationToken ct = default)
    {
        var submission = await _db.AppSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new InvalidOperationException("Submission not found.");

        if (!_extraction.IsConfigured)
        {
            throw new InvalidOperationException("AI extraction is not configured yet.");
        }

        // Clear any prior draft indicators from an earlier failed/retried analysis.
        var priorDrafts = await _db.AppIndicators
            .Where(i => i.AppSubmissionId == submissionId && !i.IsApproved)
            .ToListAsync();
        _db.AppIndicators.RemoveRange(priorDrafts);

        try
        {
            var pdfBytes = await _storage.DownloadAsync(submission.FileUrl, ct);
            var result = await _extraction.ExtractAsync(pdfBytes, ct);

            var kpis = await _db.EntityKpis
                .Where(k => k.EntityId == submission.EntityId)
                .Select(k => new { k.Id, k.KpiName })
                .ToListAsync(ct);

            var candidates = kpis.Select(k => (k.Id, k.KpiName));

            foreach (var extracted in result.Indicators)
            {
                var (entityKpiId, confidence) = IndicatorMatcher.FindMatch(extracted.Name, candidates);

                var indicator = new Data.Entities.AppIndicator
                {
                    Id = Guid.NewGuid(),
                    AppSubmissionId = submissionId,
                    EntityId = submission.EntityId,
                    EntityKpiId = entityKpiId,
                    Name = extracted.Name,
                    AnnualTarget = extracted.AnnualTarget,
                    Unit = extracted.Unit,
                    MatchConfidence = confidence,
                    IsApproved = false,
                    Status = "not_started",
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                _db.AppIndicators.Add(indicator);
            }

            submission.Status = "ai_processed";
            submission.AiSummary = result.Summary;
            submission.AiProcessedAt = DateTimeOffset.UtcNow;
            submission.AiProcessedBy = processedBy;

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            submission.Status = "ai_failed";
            submission.AiSummary = $"Analysis failed: {ex.Message}";
            submission.AiProcessedAt = DateTimeOffset.UtcNow;
            submission.AiProcessedBy = processedBy;
            await _db.SaveChangesAsync(ct);
        }

        return (await BuildResponseAsync(submission))!;
    }

    public async Task<AppSubmissionResponseDto> ApproveAsync(Guid submissionId, ApproveAppSubmissionDto dto, Guid reviewedBy)
    {
        var submission = await _db.AppSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new InvalidOperationException("Submission not found.");

        if (submission.Status != "ai_processed")
        {
            throw new InvalidOperationException("Only an analyzed submission can be approved.");
        }

        var draftIndicators = await _db.AppIndicators
            .Where(i => i.AppSubmissionId == submissionId && !i.IsApproved)
            .ToListAsync();

        var keepIds = dto.KeepIndicatorIds.ToHashSet();
        var toDiscard = draftIndicators.Where(i => !keepIds.Contains(i.Id)).ToList();
        var toKeep = draftIndicators.Where(i => keepIds.Contains(i.Id)).ToList();

        _db.AppIndicators.RemoveRange(toDiscard);

        foreach (var indicator in toKeep)
        {
            indicator.IsApproved = true;

            for (short quarter = 1; quarter <= 4; quarter++)
            {
                _db.AppIndicatorQuarters.Add(new Data.Entities.AppIndicatorQuarter
                {
                    Id = Guid.NewGuid(),
                    AppIndicatorId = indicator.Id,
                    Quarter = quarter,
                    Status = "not_started",
                });
            }

            if (indicator.EntityKpiId.HasValue)
            {
                var kpi = await _db.EntityKpis.FirstOrDefaultAsync(k => k.Id == indicator.EntityKpiId.Value);
                if (kpi is not null)
                {
                    kpi.Status = "received";
                    kpi.ReceivedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        submission.Status = "approved";
        submission.ReviewedBy = reviewedBy;
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await BuildResponseAsync(submission))!;
    }

    public async Task<AppSubmissionResponseDto> RejectAsync(Guid submissionId, RejectAppSubmissionDto dto, Guid reviewedBy)
    {
        var submission = await _db.AppSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new InvalidOperationException("Submission not found.");

        submission.Status = "rejected";
        submission.RejectionReason = dto.Reason;
        submission.ReviewedBy = reviewedBy;
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return (await BuildResponseAsync(submission))!;
    }

    private async Task<AppSubmissionResponseDto> BuildResponseAsync(Data.Entities.AppSubmission submission)
    {
        var indicators = await _db.AppIndicators
            .Where(i => i.AppSubmissionId == submission.Id)
            .Select(i => new AppIndicatorResponseDto
            {
                Id = i.Id,
                EntityKpiId = i.EntityKpiId,
                Name = i.Name,
                AnnualTarget = i.AnnualTarget,
                Unit = i.Unit,
                MatchConfidence = i.MatchConfidence,
                IsApproved = i.IsApproved,
                Status = i.Status,
            })
            .ToListAsync();

        return new AppSubmissionResponseDto
        {
            Id = submission.Id,
            EntityId = submission.EntityId,
            FileUrl = submission.FileUrl,
            UploadedAt = submission.UploadedAt,
            Status = submission.Status,
            AiSummary = submission.AiSummary,
            RejectionReason = submission.RejectionReason,
            Indicators = indicators,
        };
    }
}
