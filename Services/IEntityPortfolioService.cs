using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IEntityPortfolioService
{
    Task<List<EntityPortfolioDto>> ListPortfolioAsync();
    Task<EntityDetailDto?> GetEntityDetailAsync(Guid entityId);
    Task<EntityDetailDto?> GetEntityDetailBySlugAsync(string slug);
}
