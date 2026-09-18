using DsacReporting.Api.Auth;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/app-submissions")]
public class AppSubmissionsController : ControllerBase
{
    private readonly IAppSubmissionService _submissions;
    private readonly IDocumentStorageService _storage;

    public AppSubmissionsController(IAppSubmissionService submissions, IDocumentStorageService storage)
    {
        _submissions = submissions;
        _storage = storage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] Guid entityId)
    {
        if (!User.IsDsacStaff() && User.GetEntityId() != entityId)
        {
            return Forbid();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided." });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF files are accepted for an APP submission." });
        }

        if (!_storage.IsConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Document storage is not configured yet." });
        }

        await using var stream = file.OpenReadStream();
        var uploaded = await _storage.UploadAsync(stream, file.FileName, file.ContentType);

        var id = await _submissions.UploadAsync(entityId, uploaded.FileUrl, User.GetUserId());
        return Created($"/api/app-submissions/{id}", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? entityId, [FromQuery] string? status)
    {
        var effectiveEntityId = User.IsDsacStaff() ? entityId : User.GetEntityId();

        if (!User.IsDsacStaff() && effectiveEntityId is null)
        {
            return Ok(Array.Empty<object>());
        }

        var result = await _submissions.ListAsync(effectiveEntityId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var submission = await _submissions.GetAsync(id);
        if (submission is null)
        {
            return NotFound();
        }

        if (!User.IsDsacStaff() && User.GetEntityId() != submission.EntityId)
        {
            return Forbid();
        }

        return Ok(submission);
    }

    [HttpPost("{id:guid}/analyze")]
    public async Task<IActionResult> Analyze(Guid id, CancellationToken ct)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        try
        {
            var result = await _submissions.AnalyzeAsync(id, User.GetUserId(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not configured"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveAppSubmissionDto dto)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        try
        {
            var result = await _submissions.ApproveAsync(id, dto, User.GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAppSubmissionDto dto)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var result = await _submissions.RejectAsync(id, dto, User.GetUserId());
        return Ok(result);
    }
}
