using DsacReporting.Api.Auth;
using DsacReporting.Api.Data;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DsacController : ControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly ISubmissionService _submissions;
    private readonly IAppIndicatorService _indicators;
    private readonly AppDbContext _db;

    public DsacController(
        IDashboardService dashboard, ISubmissionService submissions, IAppIndicatorService indicators, AppDbContext db)
    {
        _dashboard = dashboard;
        _submissions = submissions;
        _indicators = indicators;
        _db = db;
    }

    [HttpGet("portfolio-summary")]
    public async Task<IActionResult> PortfolioSummary()
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var summary = await _dashboard.GetPortfolioSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("entity/{id}/drilldown")]
    public async Task<IActionResult> EntityDrillDown(Guid id)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var entity = await _db.Entities.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
        {
            return NotFound();
        }

        var submissions = await _submissions.ListSubmissionsAsync(id);

        return Ok(new
        {
            entityId = entity.Id,
            entityName = entity.Name,
            submissions,
        });
    }

    [HttpGet("indicator-summary")]
    public async Task<IActionResult> IndicatorSummary()
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var summary = await _indicators.GetPortfolioSummaryAsync();
        return Ok(summary);
    }
}
