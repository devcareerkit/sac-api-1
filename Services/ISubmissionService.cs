using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface ISubmissionService
{
    Task<Guid> CreateSubmissionAsync(SubmissionDto dto);
}
