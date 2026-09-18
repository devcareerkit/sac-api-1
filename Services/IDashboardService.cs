using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IDashboardService
{
    Task<PortfolioSummaryDto> GetPortfolioSummaryAsync();
}
