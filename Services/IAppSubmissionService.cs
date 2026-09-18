using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IAppSubmissionService
{
    Task<Guid> UploadAsync(Guid entityId, string fileUrl, Guid uploadedBy);
    Task<List<AppSubmissionSummaryDto>> ListAsync(Guid? entityId, string? status);
    Task<AppSubmissionResponseDto?> GetAsync(Guid submissionId);
    Task<AppSubmissionResponseDto> AnalyzeAsync(Guid submissionId, Guid processedBy, CancellationToken ct = default);
    Task<AppSubmissionResponseDto> ApproveAsync(Guid submissionId, ApproveAppSubmissionDto dto, Guid reviewedBy);
    Task<AppSubmissionResponseDto> RejectAsync(Guid submissionId, RejectAppSubmissionDto dto, Guid reviewedBy);
}
