using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Thekdar.Controllers;

[Authorize]
public class PayrollController : Controller
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(IPayrollService payrollService, ILogger<PayrollController> logger)
    {
        _payrollService = payrollService;
        _logger = logger;
    }

    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private bool IsAdmin => User.IsInRole("Admin");

    public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate, string? employeeFilter)
    {
        try
        {
            var payrolls = await _payrollService.GetAllPayrollRecordsAsync(IsAdmin ? null : CurrentUserId);
            
            if (startDate.HasValue) payrolls = payrolls.Where(p => p.PeriodStart >= startDate.Value).ToList();
            if (endDate.HasValue) payrolls = payrolls.Where(p => p.PeriodEnd <= endDate.Value).ToList();
            if (!string.IsNullOrEmpty(employeeFilter)) payrolls = payrolls.Where(p => p.Employee != null && p.Employee.FullName.Contains(employeeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.EmployeeFilter = employeeFilter;
            ViewBag.IsAdmin = IsAdmin;

            return View(payrolls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading payroll history");
            TempData["Error"] = "Unable to load payroll history";
            return View(new List<PayrollModel>());
        }
    }

    public async Task<IActionResult> Payslip(int id, bool print = false)
    {
        try
        {
            var payroll = await _payrollService.GetPayrollByIdAsync(id);
            if (payroll == null) return NotFound();
            if (!IsAdmin && payroll.ContractorId != CurrentUserId) return Forbid();

            var payslip = await _payrollService.GeneratePayslipAsync(id);
            ViewBag.AutoPrint = print;
            return View(payslip);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payslip");
            TempData["Error"] = "Unable to generate payslip";
            return RedirectToAction(nameof(History));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(int id, DateTime paymentDate, string paymentMethod, string transactionReference)
    {
        try
        {
            var payroll = await _payrollService.GetPayrollByIdAsync(id);
            if (!IsAdmin && payroll?.ContractorId != CurrentUserId) return Forbid();

            await _payrollService.MarkAsPaidAsync(id, paymentDate, paymentMethod, transactionReference);
            TempData["Success"] = "Payment recorded successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payment");
            TempData["Error"] = "Unable to record payment";
        }
        return RedirectToAction(nameof(History));
    }

    public async Task<IActionResult> Statistics(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            var stats = await _payrollService.GetStatisticsAsync(IsAdmin ? null : CurrentUserId, startDate, endDate);
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading statistics");
            TempData["Error"] = "Unable to load statistics";
            return View(new PayrollStatisticsViewModel());
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _payrollService.DeletePayrollRecordAsync(id);
            TempData["Success"] = "Payroll record deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll");
            TempData["Error"] = "Unable to delete record.";
        }
        return RedirectToAction(nameof(History));
    }
}
