using Microsoft.AspNetCore.Mvc;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntityController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        return Ok(new { Id = id, Name = "Sample Entity" });
    }

    [HttpPost("submit")]
    public IActionResult Submit([FromBody] object payload)
    {
        return Accepted();
    }
}
