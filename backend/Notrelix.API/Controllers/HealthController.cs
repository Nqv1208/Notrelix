using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Interfaces;

namespace Notrelix.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HealthController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var health = new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Services = new Dictionary<string, ServiceHealth>()
        };

        // Check Database
        try
        {
            var canConnect = await _context.Users.AnyAsync() || true;
            health.Services["database"] = new ServiceHealth
            {
                Status = "Healthy",
                Message = "PostgreSQL connection OK"
            };
        }
        catch (Exception ex)
        {
            health.Status = "Unhealthy";
            health.Services["database"] = new ServiceHealth
            {
                Status = "Unhealthy",
                Message = ex.Message
            };
        }

        return health.Status == "Healthy" ? Ok(health) : StatusCode(503, health);
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            await _context.Users.AnyAsync();
            return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return StatusCode(503, new { status = "not ready", timestamp = DateTime.UtcNow });
        }
    }
}

public class HealthStatus
{
    public required string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, ServiceHealth> Services { get; set; } = new();
}

public class ServiceHealth
{
    public required string Status { get; set; }
    public string? Message { get; set; }
}
