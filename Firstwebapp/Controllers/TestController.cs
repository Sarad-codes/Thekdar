using Microsoft.AspNetCore.Mvc;
using Thekdar.Data;

namespace Thekdar.Controllers.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { 
            message = "pong", 
            timestamp = DateTime.UtcNow,
            status = "API is working!"
        });
    }

    [HttpGet("env")]
    public IActionResult GetEnvironment()
    {
        return Ok(new
        {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Not set",
            machineName = Environment.MachineName,
            time = DateTime.UtcNow,
            isProduction = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development") ?? true
        });
    }

    [HttpGet("db-check")]
    public async Task<IActionResult> CheckDatabase([FromServices] ApplicationDbContext context)
    {
        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            return Ok(new { 
                databaseConnected = canConnect,
                message = canConnect ? "Database connection successful" : "Database connection failed"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                databaseConnected = false, 
                error = ex.Message 
            });
        }
    }
}