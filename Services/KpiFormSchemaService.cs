using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Services;

public class KpiFormSchemaService : IKpiFormSchemaService
{
    private readonly AppDbContext _db;
    public KpiFormSchemaService(AppDbContext db) => _db = db;

    public async Task<List<KpiFormSchemaResponseDto>> ListAsync()
    {
        return await _db.KpiFormSchemas
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new KpiFormSchemaResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                SchemaJson = s.SchemaJson,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<KpiFormSchemaResponseDto> CreateAsync(CreateKpiFormSchemaDto dto, Guid createdBy)
    {
        var schema = new Data.Entities.KpiFormSchema
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            SchemaJson = dto.SchemaJson,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        };

        _db.KpiFormSchemas.Add(schema);
        await _db.SaveChangesAsync();

        return new KpiFormSchemaResponseDto
        {
            Id = schema.Id,
            Name = schema.Name,
            SchemaJson = schema.SchemaJson,
            IsActive = schema.IsActive,
            CreatedAt = schema.CreatedAt,
        };
    }
}
