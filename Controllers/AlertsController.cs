using DsacReporting.Api.Auth;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IAlertsService _alerts;
    public AlertsController(IAlertsService alerts) => _alerts = alerts;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var alerts = await _alerts.GetActiveAlertsAsync();
        return Ok(alerts);
    }
}
