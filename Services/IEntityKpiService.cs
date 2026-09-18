using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IEntityKpiService
{
    Task<List<EntityKpiResponseDto>> ListForEntityAsync(Guid entityId);
    Task<List<EntityKpiResponseDto>> SubmitAsync(Guid entityId, SubmitEntityKpisDto dto, Guid createdBy);
    Task<int> SendAsync(Guid entityId);
}
