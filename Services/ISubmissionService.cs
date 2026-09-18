using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface ISubmissionService
{
    Task<Guid> CreateSubmissionAsync(SubmissionDto dto, Guid submittedBy);
    Task<List<SubmissionSummaryDto>> ListSubmissionsAsync(Guid? entityId);
}
