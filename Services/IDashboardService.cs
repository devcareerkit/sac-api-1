namespace DsacReporting.Api.Services;

public interface IDashboardService
{
    Task<object> GetPortfolioSummaryAsync();
}
