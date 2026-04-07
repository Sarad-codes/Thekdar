using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Thekdar.Models;
using Thekdar.Services.Interface;

namespace Thekdar.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserService _userService;
    private readonly IJobService _jobService;
    private readonly IEmployeeService _employeeService;

    public HomeController(
        ILogger<HomeController> logger, 
        IUserService userService, 
        IJobService jobService, 
        IEmployeeService employeeService)
    {
        _logger = logger;
        _userService = userService;
        _jobService = jobService;
        _employeeService = employeeService;
    }

    public IActionResult Index()
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View("Error");
        }
    }

    public IActionResult Privacy()
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading privacy page");
            return View("Error");
        }
    }
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var activeUsers = await _userService.GetActiveUsers().ConfigureAwait(false);
            var allUsers = await _userService.GetAllUsers().ConfigureAwait(false);
            var allJobs = await _jobService.GetAllAsync().ConfigureAwait(false);
            var todaysJobs = await _jobService.GetTodaysJobsAsync().ConfigureAwait(false);
            var workers = await _employeeService.GetAllAsync().ConfigureAwait(false);

            ViewBag.ContractorCount = activeUsers?.Count ?? 0;
            ViewBag.WorkerCount = workers?.Count ?? 0;
            ViewBag.TotalUsers = allUsers?.Count ?? 0;
            ViewBag.ActiveJobs = allJobs?.Count(j => j.Status == JobStatus.Active) ?? 0;
            ViewBag.PendingJobs = allJobs?.Count(j => j.Status == JobStatus.PendingConfirmation) ?? 0;
            ViewBag.CompletedJobs = allJobs?.Count(j => j.Status == JobStatus.Completed) ?? 0;

            var workerCount = workers?.Count ?? 0;
            var activeJobCount = allJobs?.Count(j => j.Status == JobStatus.Active) ?? 0;
            _logger.LogInformation(
                "Dashboard loaded â€” Workers: {WorkerCount}, Active Jobs: {ActiveJobs}",
                workerCount, activeJobCount);

            return View(todaysJobs ?? new List<JobModel>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
        
            ViewBag.ContractorCount = 0;
            ViewBag.WorkerCount = 0;
            ViewBag.TotalUsers = 0;
            ViewBag.ActiveJobs = 0;
            ViewBag.PendingJobs = 0;
            ViewBag.CompletedJobs = 0;
        
            TempData["Error"] = "Unable to load dashboard data. Please try again.";
            return View(new List<JobModel>());
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        try
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View(new ErrorViewModel { RequestId = requestId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading error page");
            return View(new ErrorViewModel { RequestId = "Unknown" });
        }
    }
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}