using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public class SubmissionService : ISubmissionService
{
    private readonly AppDbContext _db;
    public SubmissionService(AppDbContext db) => _db = db;

    public async Task<int> CreateSubmissionAsync(SubmissionDto dto)
    {
        var s = new Data.Entities.Submission { EntityId = dto.EntityId, SubmittedAt = DateTime.UtcNow };
        _db.Submissions.Add(s);
        await _db.SaveChangesAsync();
        return s.Id;
    }
}
