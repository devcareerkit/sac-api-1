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
public class EntityController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISubmissionService _submissions;

    public EntityController(AppDbContext db, ISubmissionService submissions)
    {
        _db = db;
        _submissions = submissions;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!User.IsDsacStaff() && User.GetEntityId() != id)
        {
            return Forbid();
        }

        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        var submissions = await _submissions.ListSubmissionsAsync(id);

        return Ok(new EntityStatusDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Submissions = submissions,
        });
    }

    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmissionDto dto)
    {
        if (!User.IsDsacStaff() && User.GetEntityId() != dto.EntityId)
        {
            return Forbid();
        }

        var id = await _submissions.CreateSubmissionAsync(dto, User.GetUserId());
        return Accepted(new { id });
    }
}
