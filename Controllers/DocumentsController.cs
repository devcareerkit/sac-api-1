using DsacReporting.Api.Auth;
using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private static readonly string[] ValidDocTypes =
    {
        "strategic_plan", "app", "operational_plan", "annual_report", "quarterly_report", "financials",
    };

    private readonly IDocumentStorageService _storage;
    private readonly AppDbContext _db;

    public DocumentsController(IDocumentStorageService storage, AppDbContext db)
    {
        _storage = storage;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid entityId)
    {
        if (!User.IsDsacStaff() && User.GetEntityId() != entityId)
        {
            return Forbid();
        }

        var records = await _db.DocumentRecords
            .Where(d => d.EntityId == entityId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var documents = records.Select(d => new DocumentSummaryDto
        {
            Id = d.Id,
            Name = SlugHelper.ExtractFileName(d.FileUrl),
            UploadedAt = d.UploadedAt,
        }).ToList();

        return Ok(documents);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] Guid entityId,
        [FromForm] string docType,
        [FromForm] Guid? submissionId,
        [FromForm] Guid? kpiTagId)
    {
        if (!User.IsDsacStaff() && User.GetEntityId() != entityId)
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided." });
        }

        if (!ValidDocTypes.Contains(docType))
        {
            return BadRequest(new { error = $"docType must be one of: {string.Join(", ", ValidDocTypes)}" });
        }

        if (!_storage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Document storage is not configured yet." });
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

        var record = new Data.Entities.DocumentRecord
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            EntityId = entityId,
            DocType = docType,
            FileUrl = uploaded.FileUrl,
            Version = uploaded.Version,
            KpiTagId = kpiTagId,
            UploadedBy = User.GetUserId(),
            UploadedAt = DateTimeOffset.UtcNow,
        };

        _db.DocumentRecords.Add(record);
        await _db.SaveChangesAsync();

        return Created(uploaded.FileUrl, new DocumentUploadResponseDto
        {
            Id = record.Id,
            FileUrl = record.FileUrl,
            Version = record.Version,
        });
    }
}
