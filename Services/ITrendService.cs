using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface ITrendService
{
    // Submission-status trend across all reporting cycles for one entity.
    Task<EntityTrendDto?> GetEntityTrendAsync(Guid entityId);

    // Portfolio-wide submitted-vs-missed counts per cycle (the "year-on-year"
    // / period-over-period comparison view for Sipho's dashboard).
    Task<PortfolioTrendDto> GetPortfolioTrendAsync();
}
