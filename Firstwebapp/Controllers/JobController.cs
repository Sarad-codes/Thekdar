using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Thekdar.Controllers;

[Authorize]
public class JobController : Controller
{
    private readonly IJobService _jobService;
    private readonly IEmployeeService _employeeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobService jobService, IEmployeeService employeeService, IEmailService emailService, ILogger<JobController> logger)
    {
        _jobService = jobService;
        _employeeService = employeeService;
        _emailService = emailService;
        _logger = logger;
    }

    private int? CurrentUserId
    {
        get
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var parsed) ? parsed : null;
        }
    }

    private IActionResult? ForbidUnlessOwnsJob(JobModel job)
    {
        if (User.IsInRole("Admin"))
            return null;
        if (!CurrentUserId.HasValue)
            return Forbid();
        if (job.CreatedByUserId != CurrentUserId.Value)
            return Forbid();
        return null;
    }

    public async Task<IActionResult> Index(string sortBy = "ScheduledDate", string sortOrder = "desc", string searchTerm = "")
    {
        try
        {
            var jobs = await _jobService.GetAllWithUsersAsync();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                jobs = jobs.Where(j => 
                    (!string.IsNullOrEmpty(j.Title) && j.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(j.ClientName) && j.ClientName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(j.Address) && j.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
            
            // Apply sorting
            if (sortBy == "AssignedCount")
            {
                // Get assigned counts for all filtered jobs
                var jobCounts = new Dictionary<int, int>();
                foreach (var job in jobs)
                {
                    jobCounts[job.Id] = await _jobService.GetAssignedEmployeeCountAsync(job.Id);
                }
                
                jobs = sortOrder == "asc"
                    ? jobs.OrderBy(j => jobCounts.TryGetValue(j.Id, out var count) ? count : 0)
                           .ThenBy(j => j.ScheduledDate)
                           .ToList()
                    : jobs.OrderByDescending(j => jobCounts.TryGetValue(j.Id, out var count) ? count : 0)
                           .ThenByDescending(j => j.ScheduledDate)
                           .ToList();
            }
            else
            {
                jobs = sortBy switch
                {
                    "Title" => sortOrder == "asc" 
                        ? jobs.OrderBy(j => j.Title).ThenBy(j => j.ScheduledDate).ToList() 
                        : jobs.OrderByDescending(j => j.Title).ThenByDescending(j => j.ScheduledDate).ToList(),
                        
                    "ClientName" => sortOrder == "asc" 
                        ? jobs.OrderBy(j => j.ClientName).ThenBy(j => j.ScheduledDate).ToList() 
                        : jobs.OrderByDescending(j => j.ClientName).ThenByDescending(j => j.ScheduledDate).ToList(),
                        
                    "Address" => sortOrder == "asc" 
                        ? jobs.OrderBy(j => j.Address).ThenBy(j => j.ScheduledDate).ToList() 
                        : jobs.OrderByDescending(j => j.Address).ThenByDescending(j => j.ScheduledDate).ToList(),
                        
                    "Status" => sortOrder == "asc" 
                        ? jobs.OrderBy(j => j.Status).ThenBy(j => j.ScheduledDate).ToList() 
                        : jobs.OrderByDescending(j => j.Status).ThenByDescending(j => j.ScheduledDate).ToList(),
                        
                    _ => sortOrder == "asc" 
                        ? jobs.OrderBy(j => j.ScheduledDate).ToList() 
                        : jobs.OrderByDescending(j => j.ScheduledDate).ToList()
                };
            }
            
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;
            
            return View(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading jobs index with search/sort. SortBy: {SortBy}, SortOrder: {SortOrder}, SearchTerm: {SearchTerm}", 
                sortBy, sortOrder, searchTerm);
            TempData["Error"] = "Error loading jobs. Please try again.";
            
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.SearchTerm = searchTerm;
            
            return View(new List<JobModel>());
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var job = await _jobService.GetByIdWithUsersAsync(id.Value);
        if (job == null)
            return NotFound();

        ViewBag.JobAssignments = await _jobService.GetAssignmentsForJobAsync(id.Value);
        return View(job);
    }

    [Authorize(Roles = "Admin,Contractor")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> Create(JobModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        if (!CurrentUserId.HasValue)
            return Forbid();

        try
        {
            if (model.ScheduledDate.HasValue)
                model.ScheduledDate = DateTime.SpecifyKind(model.ScheduledDate.Value, DateTimeKind.Utc);

            await _jobService.CreateAsync(model, CurrentUserId.Value);
            TempData["Success"] = "Job created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while creating a job");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating a job");
            ModelState.AddModelError(string.Empty, "Unable to create the job right now.");
            return View(model);
        }
    }

    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var job = await _jobService.GetByIdAsync(id.Value);
        if (job == null)
            return NotFound();

        var denied = ForbidUnlessOwnsJob(job);
        if (denied != null)
            return denied;

        if (User.IsInRole("Contractor") && job.Status == JobStatus.Completed)
        {
            TempData["Error"] = "This job has been confirmed complete and cannot be edited.";
            return RedirectToAction(nameof(Index));
        }

        return View(job);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> Edit(int id, JobModel model)
    {
        if (id != model.Id)
            return BadRequest();
        if (!ModelState.IsValid)
            return View(model);

        var existing = await _jobService.GetByIdAsync(id);
        if (existing == null)
            return NotFound();

        var denied = ForbidUnlessOwnsJob(existing);
        if (denied != null)
            return denied;

        try
        {
            await _jobService.UpdateAsync(model);
            TempData["Success"] = "Job updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation failed while updating job {JobId}", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating job {JobId}", id);
            ModelState.AddModelError(string.Empty, "Unable to update the job right now.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> MarkComplete(int id)
    {
        if (!CurrentUserId.HasValue)
            return Forbid();

        var job = await _jobService.GetByIdAsync(id);
        if (job == null)
            return NotFound();

        var denied = ForbidUnlessOwnsJob(job);
        if (denied != null)
            return denied;

        try
        {
            await _jobService.MarkPendingAsync(id, CurrentUserId.Value);
            TempData["Success"] = "Job marked as complete and waiting for admin confirmation.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as complete", id);
            TempData["Error"] = "Unable to update the job status right now.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ConfirmComplete(int id)
    {
        if (!CurrentUserId.HasValue)
            return Forbid();

        try
        {
            await _jobService.ConfirmCompleteAsync(id, CurrentUserId.Value);
            TempData["Success"] = "Job confirmed as complete!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming job {JobId} as complete", id);
            TempData["Error"] = "Unable to confirm the job right now.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var job = await _jobService.GetByIdWithUsersAsync(id.Value);
        if (job == null)
            return NotFound();

        return View(job);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _jobService.DeleteAsync(id);
            TempData["Success"] = "Job deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job {JobId}", id);
            TempData["Error"] = "Unable to delete the job right now.";
        }

        Response.Headers.Append("Clear-Site-Data", "\"cache\"");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> GetAssignModalContent(int jobId)
    {
        var job = await _jobService.GetByIdAsync(jobId);
        if (job == null)
            return NotFound();

        var denied = ForbidUnlessOwnsJob(job);
        if (denied != null)
            return denied;

        var allEmployees = await _employeeService.GetActiveWorkersAsync();
        var assignedEmployeeIds = await _jobService.GetAssignedEmployeeIdsAsync(jobId);

        var viewModel = new AssignEmployeesViewModel
        {
            JobId = jobId,
            JobTitle = job.Title,
            Employees = allEmployees.Select(e => new EmployeeCheckboxViewModel
            {
                Id = e.Id,
                FullName = e.FullName,
                Trade = e.Trade,
                DailyRate = e.DailyRate,
                IsAssigned = assignedEmployeeIds.Contains(e.Id),
                SelectedRole = "Assistant"
            }).ToList()
        };

        return PartialView("_AssignEmployeesPartial", viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Contractor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployees(int jobId, List<int> employeeIds, Dictionary<int, string> roles)
    {
        if (!CurrentUserId.HasValue)
            return Forbid();

        if (employeeIds == null || !employeeIds.Any())
        {
            TempData["Error"] = "Please select at least one employee to assign.";
            return RedirectToAction(nameof(Index));
        }

        var jobForAssign = await _jobService.GetByIdWithUsersAsync(jobId);
        if (jobForAssign == null)
            return NotFound();

        var assignDenied = ForbidUnlessOwnsJob(jobForAssign);
        if (assignDenied != null)
            return assignDenied;

        try
        {
            var assignedAt = DateTime.UtcNow;
            var assignmentResult = await _jobService.AssignEmployeesAsync(
                jobId,
                employeeIds,
                roles,
                CurrentUserId.Value,
                User.IsInRole("Admin"));

            var assignedBy = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? "System";
            var successCount = 0;
            var failCount = 0;

            foreach (var employeeId in assignmentResult.AssignedEmployeeIds)
            {
                var employee = await _employeeService.GetByIdAsync(employeeId);
                if (employee == null || string.IsNullOrWhiteSpace(employee.Email))
                    continue;

                var role = roles != null && roles.TryGetValue(employeeId, out var selectedRole) ? selectedRole : "Assistant";

                try
                {
                    await _emailService.SendJobAssignmentEmailAsync(
                        employee.Email,
                        employee.FullName,
                        jobForAssign.Title,
                        jobForAssign.ClientName ?? "Client",
                        jobForAssign.Address ?? "Location to be confirmed",
                        jobForAssign.ScheduledDate,
                        assignedBy,
                        role,
                        assignedAt);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "Failed to send assignment email to {Email}", employee.Email);
                }
            }

            var message = "Employees assigned successfully!";
            if (successCount > 0)
                message += $" Notification emails sent to {successCount} worker(s).";
            if (failCount > 0)
                message += $" Failed to send to {failCount} worker(s).";

            TempData["Success"] = message;
            if (assignmentResult.Warnings.Any())
                TempData["Warning"] = string.Join(" ", assignmentResult.Warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning employees to job {JobId}", jobId);
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = jobId });
    }

    [Authorize(Roles = "Admin,Contractor")]
    public async Task<IActionResult> GetAssignedCount(int jobId)
    {
        try
        {
            var job = await _jobService.GetByIdAsync(jobId);
            if (job == null)
                return NotFound(new { count = 0 });

            var denied = ForbidUnlessOwnsJob(job);
            if (denied != null)
                return Json(new { count = 0 });

            var count = await _jobService.GetAssignedEmployeeCountAsync(jobId);
            return Json(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned count for job {JobId}", jobId);
            return Json(new { count = 0 });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnassignEmployee(int jobId, int employeeId)
    {
        try
        {
            var job = await _jobService.GetByIdAsync(jobId);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            await _jobService.UnassignEmployeeAsync(jobId, employeeId);
            TempData["Success"] = "Employee unassigned successfully!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning employee {EmployeeId} from job {JobId}", employeeId, jobId);
            TempData["Error"] = "Unable to unassign the employee right now.";
        }

        return RedirectToAction(nameof(Details), new { id = jobId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnassignAllEmployees(int jobId)
    {
        try
        {
            var job = await _jobService.GetByIdAsync(jobId);
            if (job == null)
            {
                TempData["Error"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            await _jobService.UnassignAllEmployeesAsync(jobId);
            TempData["Success"] = "All employees unassigned successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning all employees from job {JobId}", jobId);
            TempData["Error"] = "Unable to unassign employees right now.";
        }

        return RedirectToAction(nameof(Details), new { id = jobId });
    }
}