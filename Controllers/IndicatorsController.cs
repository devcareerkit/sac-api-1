using DsacReporting.Api.Auth;
using DsacReporting.Api.Data;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/indicators")]
public class IndicatorsController : ControllerBase
{
    private readonly IAppIndicatorService _indicators;
    private readonly IDocumentStorageService _storage;
    private readonly AppDbContext _db;

    public IndicatorsController(IAppIndicatorService indicators, IDocumentStorageService storage, AppDbContext db)
    {
        _indicators = indicators;
        _storage = storage;
        _db = db;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!await CanAccessAsync(id))
        {
            return Forbid();
        }

        var indicator = await _indicators.GetAsync(id);
        return indicator is null ? NotFound() : Ok(indicator);
    }

    [HttpPost("{id:guid}/quarters/{quarter:int}/proof")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> SubmitProof(
        Guid id, int quarter, [FromForm] IFormFile file, [FromForm] string? notes)
    {
        if (!await CanAccessAsync(id))
        {
            return Forbid();
        }

        if (quarter is < 1 or > 4)
        {
            return BadRequest(new { error = "quarter must be between 1 and 4." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No proof file provided." });
        }

        if (!_storage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Document storage is not configured yet." });
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

        try
        {
            var result = await _indicators.SubmitProofAsync(
                id, (short)quarter, uploaded.FileUrl, notes, User.GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private async Task<bool> CanAccessAsync(Guid indicatorId)
    {
        if (User.IsDsacStaff())
        {
            return true;
        }

        var entityId = await _db.AppIndicators
            .Where(i => i.Id == indicatorId)
            .Select(i => (Guid?)i.EntityId)
            .FirstOrDefaultAsync();

        return entityId is not null && entityId == User.GetEntityId();
    }
}
