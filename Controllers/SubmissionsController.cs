using DsacReporting.Api.Auth;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissions;
    public SubmissionsController(ISubmissionService submissions) => _submissions = submissions;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        // entity_officer only ever sees their own entity's submissions; DSAC staff see all.
        var entityId = User.IsDsacStaff() ? null : User.GetEntityId();
        var result = await _submissions.ListSubmissionsAsync(entityId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SubmissionDto dto)
    {
        var callerEntityId = User.GetEntityId();
        if (!User.IsDsacStaff() && callerEntityId != dto.EntityId)
        {
            return Forbid();
        }

        var id = await _submissions.CreateSubmissionAsync(dto, User.GetUserId());
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }
}
