using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thekdar.Data;
using System.Security.Claims;
using Thekdar.Models;

namespace Thekdar.Controllers.ApiControllers;

[ApiController]
[Route("api/mobile/[controller]")]
[Authorize(Roles = "Worker")]
public class MobileJobsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MobileJobsController> _logger;

    public MobileJobsController(ApplicationDbContext context, ILogger<MobileJobsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentWorkerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodaysJobs()
    {
        try
        {
            var workerId = GetCurrentWorkerId();
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var assignments = await _context.JobAssignments
                .Include(a => a.Job)
                .Where(a => a.EmployeeId == workerId 
                    && a.Job.ScheduledDate.HasValue 
                    && a.Job.ScheduledDate.Value.Date >= today
                    && a.Job.ScheduledDate.Value.Date < tomorrow)
                .Select(a => new MobileJobDto
                {
                    JobId = a.Job.Id,
                    Title = a.Job.Title,
                    Description = a.Job.Description,
                    ClientName = a.Job.ClientName,
                    Address = a.Job.Address,
                    ScheduledDate = a.Job.ScheduledDate,
                    Status = a.Job.Status.ToString(),
                    Role = a.Role,
                    IsCompleted = a.Job.Status == JobStatus.Completed
                })
                .ToListAsync();

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's jobs for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error fetching jobs" });
        }
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingJobs(int days = 7)
    {
        try
        {
            var workerId = GetCurrentWorkerId();
            var today = DateTime.UtcNow.Date;
            var futureDate = today.AddDays(days);

            var assignments = await _context.JobAssignments
                .Include(a => a.Job)
                .Where(a => a.EmployeeId == workerId 
                    && a.Job.ScheduledDate.HasValue 
                    && a.Job.ScheduledDate.Value.Date > today
                    && a.Job.ScheduledDate.Value.Date <= futureDate
                    && a.Job.Status != JobStatus.Completed)
                .OrderBy(a => a.Job.ScheduledDate)
                .Select(a => new MobileJobDto
                {
                    JobId = a.Job.Id,
                    Title = a.Job.Title,
                    Description = a.Job.Description,
                    ClientName = a.Job.ClientName,
                    Address = a.Job.Address,
                    ScheduledDate = a.Job.ScheduledDate,
                    Status = a.Job.Status.ToString(),
                    Role = a.Role,
                    IsCompleted = false
                })
                .ToListAsync();

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming jobs for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error fetching jobs" });
        }
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> GetJobDetails(int jobId)
    {
        try
        {
            var workerId = GetCurrentWorkerId();

            var assignment = await _context.JobAssignments
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.EmployeeId == workerId && a.JobId == jobId);

            if (assignment == null)
            {
                return NotFound(new { message = "Job not found or not assigned to you" });
            }

            return Ok(new MobileJobDetailDto
            {
                JobId = assignment.Job.Id,
                Title = assignment.Job.Title,
                Description = assignment.Job.Description,
                ClientName = assignment.Job.ClientName,
                ClientPhone = "", // Add if you have client phone field
                Address = assignment.Job.Address,
                ScheduledDate = assignment.Job.ScheduledDate,
                Status = assignment.Job.Status.ToString(),
                Role = assignment.Role,
                Notes = assignment.Job.Description
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job details for worker {WorkerId}, job {JobId}", GetCurrentWorkerId(), jobId);
            return StatusCode(500, new { message = "Error fetching job details" });
        }
    }
}

// DTOs
public class MobileJobDto
{
    public int JobId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ClientName { get; set; }
    public string Address { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; }
    public string Role { get; set; }
    public bool IsCompleted { get; set; }
}

public class MobileJobDetailDto
{
    public int JobId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ClientName { get; set; }
    public string ClientPhone { get; set; }
    public string Address { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string Status { get; set; }
    public string Role { get; set; }
    public string Notes { get; set; }
}