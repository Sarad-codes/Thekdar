using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;

namespace Thekdar.Controllers;

[Authorize(Roles = "Admin,Contractor")]
public class WageController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IPayrollService _payrollService;
    private readonly ILogger<WageController> _logger;

    public WageController(
        IEmployeeService employeeService,
        IPayrollService payrollService,
        ILogger<WageController> logger)
    {
        _employeeService = employeeService;
        _payrollService = payrollService;
        _logger = logger;
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    public async Task<IActionResult> Calculator(int? employeeId, string? searchTerm, DateTime? periodStart, DateTime? periodEnd)
    {
        var model = new PayrollCalculationViewModel
        {
            EmployeeId = employeeId,
            SearchTerm = searchTerm,
            PeriodStart = periodStart?.Date ?? DateTime.Today.AddDays(1 - DateTime.Today.Day),
            PeriodEnd = periodEnd?.Date ?? DateTime.Today
        };

        await PopulateCalculatorAsync(model).ConfigureAwait(false);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculator(PayrollCalculationViewModel model)
    {
        await PopulateCalculatorAsync(model).ConfigureAwait(false);

        if (!await ValidateCalculationRequestAsync(model).ConfigureAwait(false))
        {
            return View(model);
        }

        var employee = await GetAccessibleEmployeeAsync(model.EmployeeId!.Value).ConfigureAwait(false);
        if (employee == null)
        {
            return Forbid();
        }

        ApplyCalculatedTotals(model, employee.DailyRate);
        model.HasCalculated = true;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PayrollCalculationViewModel model)
    {
        await PopulateCalculatorAsync(model).ConfigureAwait(false);

        if (!await ValidateCalculationRequestAsync(model).ConfigureAwait(false))
        {
            return View("Calculator", model);
        }

        var employee = await GetAccessibleEmployeeAsync(model.EmployeeId!.Value).ConfigureAwait(false);
        if (employee == null)
        {
            return Forbid();
        }

        ApplyCalculatedTotals(model, employee.DailyRate);
        model.HasCalculated = true;

        try
        {
            var currentUserId = CurrentUserId;
            if (!currentUserId.HasValue)
            {
                return Challenge();
            }

            var payroll = await _payrollService.SavePayrollRecordAsync(
                    model,
                    employee.ContractorId,
                    currentUserId.Value)
                .ConfigureAwait(false);

            TempData["Success"] = $"Payroll saved for {employee.FullName}.";
            return RedirectToAction("Payslip", "Payroll", new { id = payroll.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll save rejected for employee {EmployeeId}", model.EmployeeId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Calculator", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving payroll for employee {EmployeeId}", model.EmployeeId);
            ModelState.AddModelError(string.Empty, "Unable to save payroll right now.");
            return View("Calculator", model);
        }
    }

    private async Task PopulateCalculatorAsync(PayrollCalculationViewModel model)
    {
        model.PeriodStart = model.PeriodStart == default ? DateTime.Today.AddDays(1 - DateTime.Today.Day) : model.PeriodStart.Date;
        model.PeriodEnd = model.PeriodEnd == default ? DateTime.Today : model.PeriodEnd.Date;

        var employees = await GetAccessibleEmployeesAsync().ConfigureAwait(false);
        var filteredEmployees = employees.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(model.SearchTerm))
        {
            var term = model.SearchTerm.Trim();
            filteredEmployees = filteredEmployees.Where(e =>
                e.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Trade.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Phone1.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        model.SearchResults = filteredEmployees
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .Take(25)
            .Select(e => new EmployeeSearchResult
            {
                Id = e.Id,
                Name = e.FullName,
                Trade = e.Trade,
                DailyRate = e.DailyRate,
                Phone = e.Phone1,
                IsAvailable = e.IsAvailable
            })
            .ToList();

        if (!model.EmployeeId.HasValue)
        {
            return;
        }

        var selectedEmployee = employees.FirstOrDefault(e => e.Id == model.EmployeeId.Value)
            ?? await GetAccessibleEmployeeAsync(model.EmployeeId.Value).ConfigureAwait(false);

        if (selectedEmployee == null)
        {
            return;
        }

        model.EmployeeName = selectedEmployee.FullName;
        model.EmployeeTrade = selectedEmployee.Trade;
        model.DailyRate = selectedEmployee.DailyRate;

        var workSummary = await _payrollService
            .GetWorkSummaryAsync(selectedEmployee.Id, model.PeriodStart, model.PeriodEnd)
            .ConfigureAwait(false);

        model.SuggestedDaysWorked = workSummary.DaysWorked;
        model.MatchingAssignments = workSummary.MatchingAssignments;

        if (model.DaysWorked == 0 && workSummary.DaysWorked > 0 && !model.HasCalculated)
        {
            model.DaysWorked = workSummary.DaysWorked;
        }
    }

    private async Task<bool> ValidateCalculationRequestAsync(PayrollCalculationViewModel model)
    {
        if (!model.EmployeeId.HasValue)
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "Please select a worker.");
        }

        if (model.PeriodEnd.Date < model.PeriodStart.Date)
        {
            ModelState.AddModelError(nameof(model.PeriodEnd), "Period end must be on or after the period start.");
        }

        var totalDaysInPeriod = (model.PeriodEnd.Date - model.PeriodStart.Date).Days + 1;
        if (model.DaysWorked > totalDaysInPeriod)
        {
            ModelState.AddModelError(nameof(model.DaysWorked), $"Days worked cannot exceed {totalDaysInPeriod} for the selected period.");
        }

        if (!ModelState.IsValid)
        {
            return false;
        }

        var employee = await GetAccessibleEmployeeAsync(model.EmployeeId!.Value).ConfigureAwait(false);
        if (employee == null)
        {
            ModelState.AddModelError(nameof(model.EmployeeId), "You do not have access to the selected worker.");
            return false;
        }

        return true;
    }

    private async Task<List<EmployeeModel>> GetAccessibleEmployeesAsync()
    {
        if (IsAdmin)
        {
            return await _employeeService.GetAllAsync().ConfigureAwait(false);
        }

        return CurrentUserId.HasValue
            ? await _employeeService.GetByContractorAsync(CurrentUserId.Value).ConfigureAwait(false)
            : new List<EmployeeModel>();
    }

    private async Task<EmployeeModel?> GetAccessibleEmployeeAsync(int employeeId)
    {
        var employee = await _employeeService.GetByIdAsync(employeeId).ConfigureAwait(false);
        if (employee == null || employee.IsDeleted)
        {
            return null;
        }

        if (IsAdmin || (CurrentUserId.HasValue && employee.ContractorId == CurrentUserId.Value))
        {
            return employee;
        }

        return null;
    }

    private static void ApplyCalculatedTotals(PayrollCalculationViewModel model, decimal dailyRate)
    {
        var baseWage = Math.Round(dailyRate * model.DaysWorked, 2, MidpointRounding.AwayFromZero);
        var overtimeWage = Math.Round((dailyRate / 8m) * model.OvertimeHours * model.OvertimeMultiplier, 2, MidpointRounding.AwayFromZero);
        var totalWage = Math.Round(baseWage + overtimeWage + model.Bonus, 2, MidpointRounding.AwayFromZero);
        var netPayable = Math.Round(totalWage - model.Deduction, 2, MidpointRounding.AwayFromZero);

        model.DailyRate = dailyRate;
        model.BaseWage = baseWage;
        model.OvertimeWage = overtimeWage;
        model.TotalWage = totalWage;
        model.NetPayable = netPayable;
    }
}
