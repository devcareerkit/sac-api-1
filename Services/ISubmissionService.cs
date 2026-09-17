using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface ISubmissionService
{
    Task<int> CreateSubmissionAsync(SubmissionDto dto);
}
