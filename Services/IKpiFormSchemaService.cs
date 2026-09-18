using DsacReporting.Api.DTOs;

namespace DsacReporting.Api.Services;

public interface IKpiFormSchemaService
{
    Task<List<KpiFormSchemaResponseDto>> ListAsync();
    Task<KpiFormSchemaResponseDto> CreateAsync(CreateKpiFormSchemaDto dto, Guid createdBy);
}
