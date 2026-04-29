using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Thekdar.Models;
using Thekdar.Services.Interface;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Thekdar.Controllers;

[Authorize(Roles = "Admin,Contractor")]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, IEmailService emailService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _emailService = emailService;
        _logger = logger;
    }

    private int CurrentContractorId
    {
        get
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var parsed))
                return 0;
            return parsed;
        }
    }

    private bool IsAdmin => User.IsInRole("Admin");

    private IActionResult? ForbidUnlessOwnsEmployee(EmployeeModel employee)
    {
        if (IsAdmin) return null;
        if (employee.ContractorId != CurrentContractorId) return Forbid();
        return null;
    }

    public async Task<IActionResult> Index(string filter = "Active")
    {
        var employees = await GetEmployeesForFilterAsync(filter);
        var viewModels = employees.Select(MapToViewModel).ToList();

        ViewBag.CurrentFilter = filter;
        return View(viewModels);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        
        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee == null) return NotFound();

        if (employee.IsDeleted)
        {
            var denied = ForbidUnlessOwnsEmployee(employee);
            if (denied != null) return denied;

            return RedirectToAction(nameof(Edit), new { id = employee.Id });
        }

        return View(MapToViewModel(employee));
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeViewModel model, string generatedPassword)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
            return View(model);
        }

        var contractorId = CurrentContractorId;
        
        if (contractorId == 0)
        {
            ModelState.AddModelError("", "Unable to identify current user. Please log in again.");
            return View(model);
        }
        
        try
        {
            var employee = await _employeeService.CreateAsync(model, contractorId);
            
            // If mobile access is enabled, send credentials
            if (model.MobileEnabled && !string.IsNullOrEmpty(generatedPassword))
            {
                await _employeeService.EnableMobileAccessAsync(employee.Id, generatedPassword);
                await _emailService.SendMobileCredentialsEmailAsync(employee.Email, employee.FullName, generatedPassword);
                TempData["Info"] = "Mobile login credentials sent to worker's email.";
            }
            
            TempData["Success"] = "Employee added successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating employee");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error creating employee");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating employee");
            ModelState.AddModelError(string.Empty, "Unable to add the employee right now. Please try again.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        
        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee == null) return NotFound();

        var denied = ForbidUnlessOwnsEmployee(employee);
        if (denied != null) return denied;

        var viewModel = MapToViewModel(employee);
        
        // Generate a new password for display if mobile is enabled
        if (viewModel.MobileEnabled && string.IsNullOrEmpty(viewModel.GeneratedPassword))
        {
            viewModel.GeneratedPassword = GenerateRandomPassword(viewModel.FirstName);
        }
        
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var existing = await _employeeService.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var denied = ForbidUnlessOwnsEmployee(existing);
        if (denied != null) return denied;

        try
        {
            await _employeeService.UpdateAsync(model);
            TempData["Success"] = "Employee updated successfully!";
            return RedirectToAction(nameof(Index), new { filter = GetListFilter(existing) });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating employee");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business validation error updating employee");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating employee");
            ModelState.AddModelError(string.Empty, "Unable to update the employee right now.");
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
    
        var employee = await _employeeService.GetByIdAsync(id.Value);
        if (employee == null) return NotFound();
    
        var activeAssignments = await _employeeService.GetActiveAssignmentsWithDetailsAsync(id.Value);
    
        var model = MapToViewModel(employee);
    
        ViewBag.ActiveAssignments = activeAssignments;
        ViewBag.HasActiveAssignments = activeAssignments.Any();
    
        return View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SoftDeleteConfirmed(int id)
    {
        try
        {
            await _employeeService.SoftDeleteAsync(id);
            TempData["Success"] = "Employee deactivated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error deactivating employee");
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deactivating employee");
            TempData["Error"] = "Unable to deactivate the employee right now.";
        }
    
        Response.Headers.Append("Clear-Site-Data", "\"cache\"");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(int id)
    {
        try
        {
            await _employeeService.ReactivateAsync(id);
            TempData["Success"] = "Employee reactivated successfully.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error reactivating employee");
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reactivating employee");
            TempData["Error"] = "Unable to reactivate the employee right now.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ========== MOBILE ACCESS ACTIONS ==========

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMobileCredentials(int id, string password)
    {
        try
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
                return Json(new { success = false, message = "Worker not found" });
            
            if (string.IsNullOrEmpty(employee.Email))
                return Json(new { success = false, message = "Worker must have an email address to receive credentials." });
            
            await _employeeService.EnableMobileAccessAsync(id, password);
            await _emailService.SendMobileCredentialsEmailAsync(employee.Email, employee.FullName, password);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending mobile credentials for employee {EmployeeId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetMobilePassword(int id, string password)
    {
        try
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
                return Json(new { success = false, message = "Worker not found" });
    
            if (string.IsNullOrEmpty(employee.Email))
                return Json(new { success = false, message = "Worker must have an email address." });
    
            await _employeeService.EnableMobileAccessAsync(id, password);
            await _emailService.SendMobileCredentialsEmailAsync(employee.Email, employee.FullName, password);
    
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting mobile password for employee {EmployeeId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeMobileAccess(int id)
    {
        try
        {
            var employee = await _employeeService.GetByIdAsync(id);
            if (employee == null)
                return Json(new { success = false, message = "Worker not found" });
            
            await _employeeService.DisableMobileAccessAsync(id);
            
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking mobile access for employee {EmployeeId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ========== HELPER METHODS ==========

    private async Task<List<EmployeeModel>> GetEmployeesForFilterAsync(string filter)
    {
        return filter switch
        {
            "Active" => await _employeeService.GetActiveWorkersAsync(),
            "Deleted" => await _employeeService.GetDeletedWorkersAsync(),
            _ => await _employeeService.GetAllWorkersAsync("All")
        };
    }

    private static string GetListFilter(EmployeeModel employee) => employee.IsDeleted ? "Deleted" : "Active";

    private string GenerateRandomPassword(string firstName)
    {
        var randomNum = new Random().Next(100, 999);
        return $"{firstName}@{randomNum}";
    }

    private EmployeeViewModel MapToViewModel(EmployeeModel employee)
    {
        return new EmployeeViewModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Trade = employee.Trade,
            Phone = employee.Phone1,
            Phone2 = employee.Phone2,
            Email = employee.Email,
            DailyRate = employee.DailyRate,
            IsAvailable = employee.IsAvailable,
            IsDeleted = employee.IsDeleted,
            PanNumber = employee.PanNumber,
            HireDate = employee.HireDate,
            ExistingPanCardPath = employee.PanCardImagePath,
            ExistingProfilePicturePath = employee.ProfilePicturePath,
            ContractorId = employee.ContractorId,
            ContractorName = employee.Contractor?.Name,
            
            // ========== MOBILE ACCESS PROPERTIES ==========
            MobileEnabled = employee.MobileEnabled,
            MobileEnabledWas = employee.MobileEnabled,
            LastMobileLogin = employee.LastMobileLogin?.ToString("MMM dd, yyyy h:mm tt") ?? "Never",
            MobilePasswordHash = employee.MobilePasswordHash,
            GeneratedPassword = employee.MobileEnabled ? GenerateRandomPassword(employee.FirstName) : null
        };
    }
}