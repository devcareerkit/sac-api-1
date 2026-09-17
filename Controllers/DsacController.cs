using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DsacController : ControllerBase
{
    [HttpGet("portfolio-summary")]
    public IActionResult PortfolioSummary()
    {
        return Ok(new { total = 0, ok = 0, atRisk = 0 });
    }

    [HttpGet("entity/{id}/drilldown")]
    public IActionResult EntityDrillDown(int id)
    {
        return Ok(new { entityId = id });
    }
}
