using DsacReporting.Api.Auth;
using DsacReporting.Api.Data;
using DsacReporting.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DsacReporting.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    public TasksController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        // entity_officer sees only tasks assigned to them; DSAC staff see everything.
        var query = _db.TaskItems.AsQueryable();

        if (!User.IsDsacStaff())
        {
            var userId = User.GetUserId();
            query = query.Where(t => t.AssignedTo == userId);
        }

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                SubmissionId = t.SubmissionId,
                AssignedBy = t.AssignedBy,
                AssignedTo = t.AssignedTo,
                Description = t.Description,
                Status = t.Status,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var task = new Data.Entities.TaskItem
        {
            Id = Guid.NewGuid(),
            SubmissionId = dto.SubmissionId,
            AssignedBy = User.GetUserId(),
            AssignedTo = dto.AssignedTo,
            Description = dto.Description,
            Status = "open",
            DueDate = dto.DueDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        return Created($"/api/tasks/{task.Id}", new TaskResponseDto
        {
            Id = task.Id,
            SubmissionId = task.SubmissionId,
            AssignedBy = task.AssignedBy,
            AssignedTo = task.AssignedTo,
            Description = task.Description,
            Status = task.Status,
            DueDate = task.DueDate,
            CreatedAt = task.CreatedAt,
        });
    }
}
