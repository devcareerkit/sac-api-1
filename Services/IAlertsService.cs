using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IAlertsService
{
    Task<List<AlertDto>> GetActiveAlertsAsync();
}
