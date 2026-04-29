using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thekdar.Data;
using Thekdar.Models;
using System.Security.Claims;

namespace Thekdar.Controllers.ApiControllers;

[ApiController]
[Route("api/mobile/[controller]")]
[Authorize(Roles = "Worker")]
public class MobileAttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MobileAttendanceController> _logger;

    public MobileAttendanceController(ApplicationDbContext context, ILogger<MobileAttendanceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentWorkerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }

    [HttpPost("clockin")]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
    {
        try
        {
            var workerId = GetCurrentWorkerId();

            // Check if already clocked in to this job today
            var today = DateTime.UtcNow.Date;
            var existing = await _context.MobileAttendances
                .FirstOrDefaultAsync(a => a.WorkerId == workerId 
                    && a.JobId == request.JobId 
                    && a.ClockInTime.Date == today
                    && a.ClockOutTime == null);

            if (existing != null)
            {
                return BadRequest(new { message = "Already clocked in to this job today" });
            }

            var attendance = new MobileAttendance
            {
                WorkerId = workerId,
                JobId = request.JobId,
                ClockInTime = DateTime.UtcNow,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Notes = request.Notes,
                PhotoPath = request.PhotoPath,
                IsSynced = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.MobileAttendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(new ClockInResponse
            {
                AttendanceId = attendance.Id,
                ClockInTime = attendance.ClockInTime,
                Message = "Clocked in successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clocking in for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error recording clock in" });
        }
    }

    [HttpPost("clockout")]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request)
    {
        try
        {
            var workerId = GetCurrentWorkerId();

            var attendance = await _context.MobileAttendances
                .FirstOrDefaultAsync(a => a.Id == request.AttendanceId && a.WorkerId == workerId);

            if (attendance == null)
            {
                return NotFound(new { message = "Attendance record not found" });
            }

            if (attendance.ClockOutTime != null)
            {
                return BadRequest(new { message = "Already clocked out" });
            }

            attendance.ClockOutTime = DateTime.UtcNow;
            attendance.OutLatitude = request.Latitude;
            attendance.OutLongitude = request.Longitude;
            attendance.OutPhotoPath = request.PhotoPath;

            await _context.SaveChangesAsync();

            // Calculate hours worked
            var hoursWorked = (attendance.ClockOutTime.Value - attendance.ClockInTime).TotalHours;

            return Ok(new ClockOutResponse
            {
                AttendanceId = attendance.Id,
                ClockOutTime = attendance.ClockOutTime.Value,
                HoursWorked = Math.Round(hoursWorked, 2),
                Message = "Clocked out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clocking out for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error recording clock out" });
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        try
        {
            var workerId = GetCurrentWorkerId();
            var today = DateTime.UtcNow.Date;

            var attendance = await _context.MobileAttendances
                .Include(a => a.Job)
                .Where(a => a.WorkerId == workerId && a.ClockInTime.Date == today)
                .OrderBy(a => a.ClockInTime)
                .Select(a => new AttendanceRecordDto
                {
                    Id = a.Id,
                    JobId = a.JobId,
                    JobTitle = a.Job.Title,
                    JobAddress = a.Job.Address,
                    ClockInTime = a.ClockInTime,
                    ClockOutTime = a.ClockOutTime,
                    HoursWorked = a.ClockOutTime != null 
                        ? Math.Round((a.ClockOutTime.Value - a.ClockInTime).TotalHours, 2) 
                        : (double?)null,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(attendance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's attendance for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error fetching attendance" });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetAttendanceHistory(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            var workerId = GetCurrentWorkerId();
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var attendance = await _context.MobileAttendances
                .Include(a => a.Job)
                .Where(a => a.WorkerId == workerId 
                    && a.ClockInTime.Date >= from.Date 
                    && a.ClockInTime.Date <= to.Date)
                .OrderByDescending(a => a.ClockInTime)
                .Select(a => new AttendanceHistoryDto
                {
                    Id = a.Id,
                    Date = a.ClockInTime.Date,
                    JobTitle = a.Job.Title,
                    ClockInTime = a.ClockInTime,
                    ClockOutTime = a.ClockOutTime,
                    HoursWorked = a.ClockOutTime != null 
                        ? Math.Round((double)(a.ClockOutTime.Value - a.ClockInTime).TotalHours, 2) 
                        : null,
                    IsComplete = a.ClockOutTime != null
                })
                .ToListAsync();

            // Group by date
            var grouped = attendance
                .GroupBy(a => a.Date)
                .Select(g => new AttendanceDayDto
                {
                    Date = g.Key,
                    TotalHours = g.Sum(a => a.HoursWorked ?? 0),
                    Records = g.ToList()
                })
                .OrderByDescending(d => d.Date)
                .ToList();

            return Ok(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance history for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error fetching history" });
        }
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAttendance([FromBody] List<SyncAttendanceDto> offlineRecords)
    {
        try
        {
            var workerId = GetCurrentWorkerId();

            foreach (var record in offlineRecords)
            {
                // Check if already synced
                var existing = await _context.MobileAttendances
                    .FirstOrDefaultAsync(a => a.WorkerId == workerId 
                        && a.ClockInTime == record.ClockInTime 
                        && a.JobId == record.JobId);

                if (existing == null)
                {
                    var attendance = new MobileAttendance
                    {
                        WorkerId = workerId,
                        JobId = record.JobId,
                        ClockInTime = record.ClockInTime,
                        ClockOutTime = record.ClockOutTime,
                        Latitude = record.Latitude,
                        Longitude = record.Longitude,
                        Notes = record.Notes,
                        IsSynced = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.MobileAttendances.Add(attendance);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Synced {offlineRecords.Count} records" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing attendance for worker {WorkerId}", GetCurrentWorkerId());
            return StatusCode(500, new { message = "Error syncing attendance" });
        }
    }
}

// Request/Response Models
public class ClockInRequest
{
    public int JobId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Notes { get; set; }
    public string PhotoPath { get; set; }
}

public class ClockInResponse
{
    public int AttendanceId { get; set; }
    public DateTime ClockInTime { get; set; }
    public string Message { get; set; }
}

public class ClockOutRequest
{
    public int AttendanceId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string PhotoPath { get; set; }
}

public class ClockOutResponse
{
    public int AttendanceId { get; set; }
    public DateTime ClockOutTime { get; set; }
    public double HoursWorked { get; set; }
    public string Message { get; set; }
}

public class AttendanceRecordDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; }
    public string JobAddress { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public double? HoursWorked { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Notes { get; set; }
}

public class AttendanceHistoryDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string JobTitle { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public double? HoursWorked { get; set; }
    public bool IsComplete { get; set; }
}

public class AttendanceDayDto
{
    public DateTime Date { get; set; }
    public double TotalHours { get; set; }
    public List<AttendanceHistoryDto> Records { get; set; }
}

public class SyncAttendanceDto
{
    public int JobId { get; set; }
    public DateTime ClockInTime { get; set; }
    public DateTime? ClockOutTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Notes { get; set; }
}