using DsacReporting.Api.Auth;
using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public CommentsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? submissionId, [FromQuery] Guid? documentId)
    {
        var query = _db.Comments.AsQueryable();

        if (submissionId.HasValue)
        {
            query = query.Where(c => c.SubmissionId == submissionId);
        }

        if (documentId.HasValue)
        {
            query = query.Where(c => c.DocumentId == documentId);
        }

        var comments = await query
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                SubmissionId = c.SubmissionId,
                AuthorId = c.AuthorId,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateCommentDto dto)
    {
        var comment = new Data.Entities.Comment
        {
            Id = Guid.NewGuid(),
            DocumentId = dto.DocumentId,
            SubmissionId = dto.SubmissionId,
            AuthorId = User.GetUserId(),
            Body = dto.Body,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return Created($"/api/comments/{comment.Id}", new CommentResponseDto
        {
            Id = comment.Id,
            DocumentId = comment.DocumentId,
            SubmissionId = comment.SubmissionId,
            AuthorId = comment.AuthorId,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
        });
    }
}
