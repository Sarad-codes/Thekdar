using Thekdar.Data;
using Thekdar.Models;
using Thekdar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Thekdar.Controllers;

[Authorize(Roles = "Admin")]
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ================= INDEX =================
    public async Task<IActionResult> Index(
        string tab = "jobs",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string status = "all",
        int? contractorId = null,
        string trade = "all",
        string searchTitle = "")
    {
        var startDate = ConvertToUtcStartOfDay(fromDate ?? DateTime.UtcNow.AddDays(-30));
        var endDate = ConvertToUtcEndOfDay(toDate ?? DateTime.UtcNow);

        var model = new ReportsViewModel();

        ViewBag.SearchTitle = searchTitle;
        model.SearchTitle = searchTitle;

        switch (tab)
        {
            case "jobs":
                await LoadJobsData(model, startDate, endDate, status, contractorId, searchTitle);
                break;
            case "contractors":
                await LoadContractorsData(model, startDate, endDate);
                break;
            case "workers":
                await LoadWorkersData(model, trade);
                break;
            case "assignments":
                await LoadAssignmentsData(model, startDate, endDate, status, contractorId);
                break;
        }

        ViewBag.CurrentTab = tab;
        ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
        ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");
        ViewBag.StatusFilter = status;
        ViewBag.ContractorFilter = contractorId;
        ViewBag.TradeFilter = trade;

        ViewBag.Contractors = await _context.Users
            .Where(u => u.Role == UserRole.Contractor)
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        ViewBag.Trades = await _context.Employees
            .Where(e => !e.IsDeleted && e.Trade != null && e.Trade != "")
            .Select(e => e.Trade)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return View(model);
    }

    // ================= CONTRACTOR DETAILS DASHBOARD =================
    public async Task<IActionResult> ContractorDetails(int id)
    {
        var contractor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Contractor);
        
        if (contractor == null)
            return NotFound();
        
        return View(contractor);
    }

    // ================= WORKER DETAILS DASHBOARD =================
    public async Task<IActionResult> WorkerDetails(int id)
    {
        var worker = await _context.Employees
            .Include(e => e.Contractor)
            .FirstOrDefaultAsync(e => e.Id == id);
        
        if (worker == null)
            return NotFound();
        
        return View(worker);
    }

    // ================= GET CONTRACTOR DASHBOARD DATA (AJAX) =================
    [HttpGet]
    public async Task<IActionResult> GetContractorDashboardData(int id)
    {
        try
        {
            var workers = await _context.Employees
                .Where(e => e.ContractorId == id && !e.IsDeleted)
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    FullName = e.FirstName + " " + e.LastName,
                    e.Trade,
                    Phone = e.Phone1,
                    e.DailyRate,
                    e.IsAvailable
                })
                .ToListAsync();
            
            var jobs = await _context.Jobs
                .Where(j => j.CreatedByUserId == id)
                .OrderByDescending(j => j.CreatedAt)
                .Take(20)
                .Select(j => new
                {
                    j.Id,
                    j.Title,
                    j.ClientName,
                    j.ScheduledDate,
                    Status = j.Status.ToString(),
                    WorkersAssigned = _context.JobAssignments.Count(a => a.JobId == j.Id)
                })
                .ToListAsync();
            
            var payrolls = await _context.Payrolls
                .Include(p => p.Employee)
                .Where(p => p.ContractorId == id)
                .OrderByDescending(p => p.PeriodEnd)
                .Take(20)
                .Select(p => new
                {
                    p.Id,
                    EmployeeName = p.Employee != null ? p.Employee.FirstName + " " + p.Employee.LastName : "Unknown",
                    Period = p.PeriodStart.ToString("MMM yyyy") + " - " + p.PeriodEnd.ToString("MMM yyyy"),
                    p.DaysWorked,
                    p.NetPayable,
                    Status = p.Status.ToString()
                })
                .ToListAsync();
            
            var totalJobs = jobs.Count;
            var completedJobs = jobs.Count(j => j.Status == "Completed");
            var pendingJobs = jobs.Count(j => j.Status == "PendingConfirmation");
            
            return Json(new
            {
                success = true,
                totalWorkers = workers.Count,
                totalJobs = totalJobs,
                completedJobs = completedJobs,
                pendingJobs = pendingJobs,
                workers = workers,
                jobs = jobs,
                payrolls = payrolls
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ================= GET WORKER DASHBOARD DATA (AJAX) =================
    [HttpGet]
    public async Task<IActionResult> GetWorkerDashboardData(int id)
    {
        try
        {
            var activeAssignments = await _context.JobAssignments
                .Include(a => a.Job)
                .Include(a => a.AssignedByUser)
                .Where(a => a.EmployeeId == id && a.Status != "Completed")
                .Select(a => new
                {
                    a.JobId,
                    JobTitle = a.Job != null ? a.Job.Title : "Unknown",
                    ClientName = a.Job != null ? a.Job.ClientName : "â€”",
                    Address = a.Job != null ? a.Job.Address : "â€”",
                    a.Role,
                    AssignedBy = a.AssignedByUser != null ? a.AssignedByUser.Name : "Ã¢â‚¬â€",
                    AssignedAt = a.AssignedAt.ToString("MMM dd, yyyy")
                })
                .ToListAsync();
            
            var completedAssignments = await _context.JobAssignments
                .Include(a => a.Job)
                .Include(a => a.AssignedByUser)
                .Where(a => a.EmployeeId == id && a.Status == "Completed")
                .Select(a => new
                {
                    a.JobId,
                    JobTitle = a.Job != null ? a.Job.Title : "Unknown",
                    ClientName = a.Job != null ? a.Job.ClientName : "â€”",
                    CompletedDate = a.Job != null ? a.Job.CreatedAt.ToString("MMM dd, yyyy") : "â€”",
                    a.Role,
                    AssignedBy = a.AssignedByUser != null ? a.AssignedByUser.Name : "Ã¢â‚¬â€",
                    AssignedAt = a.AssignedAt.ToString("MMM dd, yyyy")
                })
                .ToListAsync();
            
            var payrolls = await _context.Payrolls
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.PeriodEnd)
                .Select(p => new
                {
                    p.Id,
                    Period = p.PeriodStart.ToString("MMM dd") + " - " + p.PeriodEnd.ToString("MMM dd, yyyy"),
                    p.DaysWorked,
                    p.DailyRate,
                    p.NetPayable,
                    Status = p.Status.ToString()
                })
                .ToListAsync();
            
            var totalJobs = activeAssignments.Count + completedAssignments.Count;
            var completedJobs = completedAssignments.Count;
            var totalEarnings = payrolls.Where(p => p.Status == "Paid").Sum(p => p.NetPayable);
            var totalDaysWorked = payrolls.Sum(p => p.DaysWorked);
            
            return Json(new
            {
                success = true,
                totalJobs = totalJobs,
                completedJobs = completedJobs,
                totalEarnings = totalEarnings,
                totalDaysWorked = totalDaysWorked,
                activeJobs = activeAssignments,
                completedJobsList = completedAssignments,
                payrolls = payrolls
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ================= HELPERS =================
    private DateTime ConvertToUtcStartOfDay(DateTime date)
    {
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }

    private DateTime ConvertToUtcEndOfDay(DateTime date)
    {
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        return DateTime.SpecifyKind(endOfDay, DateTimeKind.Utc);
    }

    // ================= LOAD DATA METHODS =================
    private async Task LoadJobsData(ReportsViewModel model, DateTime from, DateTime to, string status, int? contractorId, string searchTitle)
    {
        var query = _context.Jobs
            .AsNoTracking()
            .Include(j => j.CreatedBy)
            .Where(j => j.CreatedAt >= from && j.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(searchTitle))
            query = query.Where(j => j.Title.ToLower().Contains(searchTitle.ToLower()));

        if (status != "all")
        {
            var s = status switch
            {
                "active" => JobStatus.Active,
                "pending" => JobStatus.PendingConfirmation,
                "completed" => JobStatus.Completed,
                _ => (JobStatus?)null
            };
            if (s.HasValue) query = query.Where(j => j.Status == s.Value);
        }

        if (contractorId.HasValue)
            query = query.Where(j => j.CreatedByUserId == contractorId);

        var jobs = await query.ToListAsync();

        var assignmentCounts = await _context.JobAssignments
            .GroupBy(a => a.JobId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        model.Jobs = jobs.Select(j => new JobReportItem
        {
            Id = j.Id,
            Title = j.Title,
            ClientName = j.ClientName ?? "â€”",
            ScheduledDate = j.ScheduledDate,
            Status = j.Status,
            CreatedBy = j.CreatedBy?.Name ?? "â€”",
            WorkersAssigned = assignmentCounts.GetValueOrDefault(j.Id, 0),
            CreatedAt = j.CreatedAt
        }).ToList();

        model.TotalJobs = model.Jobs.Count;
        model.ActiveJobs = model.Jobs.Count(j => j.Status == JobStatus.Active);
        model.CompletedJobs = model.Jobs.Count(j => j.Status == JobStatus.Completed);
        model.PendingJobs = model.Jobs.Count(j => j.Status == JobStatus.PendingConfirmation);
    }

    private async Task LoadContractorsData(ReportsViewModel model, DateTime from, DateTime to)
    {
        var contractors = await _context.Users
            .Where(u => u.Role == UserRole.Contractor)
            .OrderBy(u => u.Name)
            .ToListAsync();

        var jobs = await _context.Jobs
            .Where(j => j.CreatedAt >= from && j.CreatedAt <= to)
            .ToListAsync();

        var employees = await _context.Employees
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        model.Contractors = contractors.Select(c => new ContractorReportItem
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            JobsCreated = jobs.Count(j => j.CreatedByUserId == c.Id),
            JobsCompleted = jobs.Count(j => j.CompletedByUserId == c.Id && j.Status == JobStatus.Completed),
            WorkersManaged = employees.Count(e => e.ContractorId == c.Id),
            ActiveWorkers = employees.Count(e => e.ContractorId == c.Id && e.IsAvailable),
            Status = c.Status == UserStatus.Active ? "Active" : "Inactive",
            JoinedDate = c.CreatedAt
        }).ToList();

        model.TotalContractors = model.Contractors.Count;
        model.ActiveContractors = model.Contractors.Count(c => c.Status == "Active");
        model.InactiveContractors = model.Contractors.Count(c => c.Status == "Inactive");
    }

    private async Task LoadWorkersData(ReportsViewModel model, string trade)
    {
        var workers = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Contractor)
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        if (trade != "all" && !string.IsNullOrEmpty(trade))
            workers = workers.Where(w => w.Trade == trade).ToList();

        var assignments = await _context.JobAssignments
            .Include(a => a.Job)
            .ToListAsync();

        model.Workers = workers.Select(w => new WorkerReportItem
        {
            Id = w.Id,
            FullName = w.FullName,
            Trade = w.Trade ?? "â€”",
            Phone = w.Phone1,
            DailyRate = w.DailyRate,
            AssignedJobs = assignments.Count(a => a.EmployeeId == w.Id),
            CompletedJobs = assignments.Count(a => a.EmployeeId == w.Id && a.Job != null && a.Job.Status == JobStatus.Completed),
            ContractorName = w.Contractor?.Name ?? "â€”",
            IsAvailable = w.IsAvailable,
            ProfilePicturePath = w.ProfilePicturePath
        }).ToList();

        model.TotalWorkers = model.Workers.Count;
        model.AvailableWorkers = model.Workers.Count(w => w.IsAvailable);
        model.AssignedWorkers = model.Workers.Count(w => w.AssignedJobs > 0);
    }

    private async Task LoadAssignmentsData(ReportsViewModel model, DateTime from, DateTime to, string status, int? contractorId)
    {
        var query = _context.JobAssignments
            .AsNoTracking()
            .Include(a => a.Job)
                .ThenInclude(j => j.CreatedBy)
            .Include(a => a.Employee)
            .Where(a => a.AssignedDate >= from && a.AssignedDate <= to);

        if (status != "all")
        {
            var s = status switch
            {
                "active" => JobStatus.Active,
                "pending" => JobStatus.PendingConfirmation,
                "completed" => JobStatus.Completed,
                _ => (JobStatus?)null
            };
            if (s.HasValue)
                query = query.Where(a => a.Job != null && a.Job.Status == s.Value);
        }

        if (contractorId.HasValue)
            query = query.Where(a => a.Job != null && a.Job.CreatedByUserId == contractorId);

        var data = await query.OrderByDescending(a => a.AssignedDate).ToListAsync();

        model.Assignments = data.Select(a => new AssignmentReportItem
        {
            JobId = a.JobId,
            JobTitle = a.Job?.Title ?? "â€”",
            ClientName = a.Job?.ClientName ?? "â€”",
            WorkerName = a.Employee?.FullName ?? "â€”",
            Trade = a.Employee?.Trade ?? "â€”",
            Role = a.Role ?? "Assistant",
            AssignedDate = a.AssignedDate,
            JobStatus = a.Job?.Status switch
            {
                JobStatus.Active => "Active",
                JobStatus.PendingConfirmation => "Pending",
                JobStatus.Completed => "Completed",
                _ => "â€”"
            },
            ContractorName = a.Job?.CreatedBy?.Name ?? "â€”"
        }).ToList();

        model.TotalAssignments = model.Assignments.Count;
        model.ActiveAssignments = model.Assignments.Count(a => a.JobStatus == "Active");
        model.CompletedAssignments = model.Assignments.Count(a => a.JobStatus == "Completed");
    }

    // ================= EXPORT CSV =================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportCSV(
        string tab,
        DateTime? fromDate,
        DateTime? toDate,
        string status,
        int? contractorId,
        string trade,
        string searchTitle = "")
    {
        var startDate = ConvertToUtcStartOfDay(fromDate ?? DateTime.UtcNow.AddDays(-30));
        var endDate = ConvertToUtcEndOfDay(toDate ?? DateTime.UtcNow);
        var sb = new StringBuilder();

        switch (tab)
        {
            case "jobs":
                await ExportJobsCSV(sb, startDate, endDate, status, contractorId, searchTitle);
                break;
            case "contractors":
                await ExportContractorsCSV(sb, startDate, endDate);
                break;
            case "workers":
                await ExportWorkersCSV(sb, trade);
                break;
            case "assignments":
                await ExportAssignmentsCSV(sb, startDate, endDate, status, contractorId);
                break;
            default:
                return BadRequest();
        }

        var fileName = $"{tab}_report_{DateTime.Now:yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private async Task ExportJobsCSV(StringBuilder sb, DateTime from, DateTime to, string status, int? contractorId, string searchTitle)
    {
        sb.AppendLine("Title,Client,Scheduled Date,Status,Contractor,Workers Assigned,Created At");
        var query = _context.Jobs.AsNoTracking().Include(j => j.CreatedBy).Where(j => j.CreatedAt >= from && j.CreatedAt <= to);

        if (!string.IsNullOrWhiteSpace(searchTitle))
            query = query.Where(j => j.Title.ToLower().Contains(searchTitle.ToLower()));
        if (status != "all")
        {
            var s = status switch { "active" => JobStatus.Active, "pending" => JobStatus.PendingConfirmation, "completed" => JobStatus.Completed, _ => (JobStatus?)null };
            if (s.HasValue) query = query.Where(j => j.Status == s.Value);
        }
        if (contractorId.HasValue) query = query.Where(j => j.CreatedByUserId == contractorId);

        var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
        foreach (var job in jobs)
        {
            var workerCount = await _context.JobAssignments.CountAsync(a => a.JobId == job.Id);
            sb.AppendLine($"\"{EscapeCsv(job.Title)}\",\"{EscapeCsv(job.ClientName)}\",{job.ScheduledDate:yyyy-MM-dd},{job.Status},\"{EscapeCsv(job.CreatedBy?.Name)}\",{workerCount},{job.CreatedAt:yyyy-MM-dd}");
        }
    }

    private async Task ExportContractorsCSV(StringBuilder sb, DateTime from, DateTime to)
    {
        sb.AppendLine("Name,Email,Phone,Jobs Created,Jobs Completed,Workers Managed,Active Workers,Status,Joined Date");
        var contractors = await _context.Users.Where(u => u.Role == UserRole.Contractor).OrderBy(u => u.Name).ToListAsync();
        var jobs = await _context.Jobs.Where(j => j.CreatedAt >= from && j.CreatedAt <= to).ToListAsync();
        var employees = await _context.Employees.Where(e => !e.IsDeleted).ToListAsync();

        foreach (var c in contractors)
        {
            var jobsCreated = jobs.Count(j => j.CreatedByUserId == c.Id);
            var jobsCompleted = jobs.Count(j => j.CompletedByUserId == c.Id && j.Status == JobStatus.Completed);
            var workersManaged = employees.Count(e => e.ContractorId == c.Id);
            var activeWorkers = employees.Count(e => e.ContractorId == c.Id && e.IsAvailable);
            sb.AppendLine($"\"{EscapeCsv(c.Name)}\",\"{EscapeCsv(c.Email)}\",\"{c.Phone}\",{jobsCreated},{jobsCompleted},{workersManaged},{activeWorkers},{c.Status},{c.CreatedAt:yyyy-MM-dd}");
        }
    }

    private async Task ExportWorkersCSV(StringBuilder sb, string trade)
    {
        sb.AppendLine("Name,Trade,Phone,Daily Rate (NPR),Assigned Jobs,Completed Jobs,Contractor,Available");
        var workers = await _context.Employees.AsNoTracking().Include(e => e.Contractor).Where(e => !e.IsDeleted).ToListAsync();
        if (trade != "all" && !string.IsNullOrEmpty(trade)) workers = workers.Where(w => w.Trade == trade).ToList();
        var assignments = await _context.JobAssignments.Include(a => a.Job).ToListAsync();

        foreach (var w in workers)
        {
            var assignedJobs = assignments.Count(a => a.EmployeeId == w.Id);
            var completedJobs = assignments.Count(a => a.EmployeeId == w.Id && a.Job != null && a.Job.Status == JobStatus.Completed);
            sb.AppendLine($"\"{EscapeCsv(w.FullName)}\",\"{EscapeCsv(w.Trade)}\",\"{w.Phone1}\",{w.DailyRate},{assignedJobs},{completedJobs},\"{EscapeCsv(w.Contractor?.Name)}\",{(w.IsAvailable ? "Yes" : "No")}");
        }
    }

    private async Task ExportAssignmentsCSV(StringBuilder sb, DateTime from, DateTime to, string status, int? contractorId)
    {
        sb.AppendLine("Job Title,Client,Worker,Trade,Role,Assigned Date,Job Status,Contractor");
        var query = _context.JobAssignments.AsNoTracking().Include(a => a.Job).ThenInclude(j => j.CreatedBy).Include(a => a.Employee).Where(a => a.AssignedDate >= from && a.AssignedDate <= to);
        if (status != "all")
        {
            var s = status switch { "active" => JobStatus.Active, "pending" => JobStatus.PendingConfirmation, "completed" => JobStatus.Completed, _ => (JobStatus?)null };
            if (s.HasValue) query = query.Where(a => a.Job != null && a.Job.Status == s.Value);
        }
        if (contractorId.HasValue) query = query.Where(a => a.Job != null && a.Job.CreatedByUserId == contractorId);

        var assignments = await query.OrderByDescending(a => a.AssignedDate).ToListAsync();
        foreach (var a in assignments)
        {
            sb.AppendLine($"\"{EscapeCsv(a.Job?.Title)}\",\"{EscapeCsv(a.Job?.ClientName)}\",\"{EscapeCsv(a.Employee?.FullName)}\",\"{EscapeCsv(a.Employee?.Trade)}\",\"{EscapeCsv(a.Role)}\",{a.AssignedDate:yyyy-MM-dd},{a.Job?.Status},\"{EscapeCsv(a.Job?.CreatedBy?.Name)}\"");
        }
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            return value.Replace("\"", "\"\"");
        return value;
    }
}

