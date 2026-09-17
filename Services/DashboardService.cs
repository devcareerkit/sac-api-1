namespace DsacReporting.Api.Services;

public class DashboardService : IDashboardService
{
    public Task<object> GetPortfolioSummaryAsync()
    {
        return Task.FromResult<object>(new { total = 0, ok = 0, atRisk = 0 });
    }
}
