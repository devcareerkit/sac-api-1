using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IAppIndicatorService
{
    Task<PagedIndicatorsDto> ListForEntityAsync(Guid entityId, int page, int pageSize);
    Task<AppIndicatorDetailDto?> GetAsync(Guid indicatorId);
    Task<AppIndicatorQuarterResponseDto> SubmitProofAsync(
        Guid indicatorId, short quarter, string proofFileUrl, string? notes, Guid completedBy);
    Task<List<IndicatorPortfolioSummaryDto>> GetPortfolioSummaryAsync();
}
