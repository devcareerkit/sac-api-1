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
    public async Task<IActionResult> List([FromQuery] Guid? entityId)
    {
        // entity_officer only ever sees their own entity's submissions; DSAC staff see all
        // (or a specific entity, if they passed ?entityId=).
        var effectiveEntityId = User.IsDsacStaff() ? entityId : User.GetEntityId();
        var result = await _submissions.ListSubmissionsAsync(effectiveEntityId);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var entityId = User.IsDsacStaff() ? null : User.GetEntityId();
        var summary = await _submissions.GetSummaryAsync(entityId);
        return Ok(summary);
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

    // Simplified submission endpoint matching the frontend's KPI submission form
    // (Job Creation / Budget Spent / Variance Notes) — resolves KPI targets for
    // the caller's own entity and the current cycle server-side.
    [HttpPost("kpi-report")]
    public async Task<IActionResult> SubmitKpiReport([FromBody] KpiReportDto report)
    {
        var entityId = User.GetEntityId();
        if (entityId is null)
        {
            return BadRequest(new { error = "Only an entity officer with an assigned entity can submit a KPI report." });
        }

        var id = await _submissions.SubmitKpiReportAsync(entityId.Value, report, User.GetUserId());
        return CreatedAtAction(nameof(List), new { id }, new { id });
    }
}
