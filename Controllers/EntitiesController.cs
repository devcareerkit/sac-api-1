using DsacReporting.Api.Auth;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

// Portfolio-shaped entity views (slug, status, risk, KPIs, documents) for the
// DSAC dashboard. Distinct from EntityController, which handles submission
// actions scoped to a single entity_officer's own entity.
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EntitiesController : ControllerBase
{
    private readonly IEntityPortfolioService _portfolio;
    private readonly IEntityKpiService _kpis;
    private readonly IAppIndicatorService _indicators;

    public EntitiesController(
        IEntityPortfolioService portfolio,
        IEntityKpiService kpis,
        IAppIndicatorService indicators)
    {
        _portfolio = portfolio;
        _kpis = kpis;
        _indicators = indicators;
    }

    /// <summary>
    /// Lists entities available to the current user.
    /// DSAC staff can see all entities.
    /// Entity officers can only see their own entity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var entities = await _portfolio.ListPortfolioAsync();

        if (!User.IsDsacStaff())
        {
            var ownEntityId = User.GetEntityId();

            entities = entities
                .Where(e => e.Id == ownEntityId)
                .ToList();
        }

        return Ok(entities);
    }

    /// <summary>
    /// Gets an entity portfolio by slug.
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var detail = await _portfolio.GetEntityDetailBySlugAsync(slug);

        if (detail is null)
        {
            return NotFound();
        }

        if (!User.IsDsacStaff() &&
            User.GetEntityId() != detail.Id)
        {
            return Forbid();
        }

        return Ok(detail);
    }

    /// <summary>
    /// Lists indicators for an entity.
    /// DSAC staff can view any entity.
    /// Entity officers can only view their own entity.
    /// </summary>
    [HttpGet("{id:guid}/indicators")]
    public async Task<IActionResult> ListIndicators(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!User.IsDsacStaff() &&
            User.GetEntityId() != id)
        {
            return Forbid();
        }

        if (page < 1)
        {
            return BadRequest(new { error = "page must be greater than 0." });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                error = "pageSize must be between 1 and 100."
            });
        }

        var indicators = await _indicators.ListForEntityAsync(
            id,
            page,
            pageSize);

        return Ok(indicators);
    }

    /// <summary>
    /// Lists KPI submissions for an entity.
    /// </summary>
    [HttpGet("{id:guid}/kpis")]
    public async Task<IActionResult> ListKpis(Guid id)
    {
        if (!User.IsDsacStaff() &&
            User.GetEntityId() != id)
        {
            return Forbid();
        }

        var kpis = await _kpis.ListForEntityAsync(id);

        return Ok(kpis);
    }

    /// <summary>
    /// Submits KPIs for an entity.
    /// Restricted to DSAC staff.
    /// </summary>
    [HttpPost("{id:guid}/kpis")]
    public async Task<IActionResult> SubmitKpis(
        Guid id,
        [FromBody] SubmitEntityKpisDto dto)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var created = await _kpis.SubmitAsync(
            id,
            dto,
            User.GetUserId());

        return Ok(created);
    }

    /// <summary>
    /// Sends KPI submissions for an entity.
    /// Restricted to DSAC staff.
    /// </summary>
    [HttpPost("{id:guid}/kpis/send")]
    public async Task<IActionResult> SendKpis(Guid id)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var count = await _kpis.SendAsync(id);

        return Ok(new
        {
            sentCount = count
        });
    }
}