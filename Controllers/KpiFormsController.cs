using DsacReporting.Api.Auth;
using DsacReporting.Api.DTOs;
using DsacReporting.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/kpi-forms")]
public class KpiFormsController : ControllerBase
{
    private readonly IKpiFormSchemaService _schemas;
    public KpiFormsController(IKpiFormSchemaService schemas) => _schemas = schemas;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        return Ok(await _schemas.ListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKpiFormSchemaDto dto)
    {
        if (!User.IsDsacStaff())
        {
            return Forbid();
        }

        var created = await _schemas.CreateAsync(dto, User.GetUserId());
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }
}
