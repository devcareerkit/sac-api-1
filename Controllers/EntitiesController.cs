using DsacReporting.Api.Auth;
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
    public EntitiesController(IEntityPortfolioService portfolio) => _portfolio = portfolio;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var entities = await _portfolio.ListPortfolioAsync();

        // entity_officer only sees their own entity in this list.
        if (!User.IsDsacStaff())
        {
            var ownEntityId = User.GetEntityId();
            entities = entities.Where(e => e.Id == ownEntityId).ToList();
        }

        return Ok(entities);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var detail = await _portfolio.GetEntityDetailBySlugAsync(slug);
        if (detail is null)
        {
            return NotFound();
        }

        if (!User.IsDsacStaff() && User.GetEntityId() != detail.Id)
        {
            return Forbid();
        }

        return Ok(detail);
    }
}
