using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;

namespace Thekdar.Controllers;

[Authorize(Roles = "Admin,Contractor")]
public class InvoiceController : Controller
{
    private readonly IPayrollService _payrollService;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(IPayrollService payrollService, ILogger<InvoiceController> logger)
    {
        _payrollService = payrollService;
        _logger = logger;
    }

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private bool IsAdmin => User.IsInRole("Admin");

    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string status = "all", string? searchTerm = null)
    {
        try
        {
            var records = await _payrollService
                .GetAllPayrollRecordsAsync(IsAdmin ? null : CurrentUserId)
                .ConfigureAwait(false);

            if (startDate.HasValue)
            {
                var normalizedStart = startDate.Value.Date;
                records = records.Where(p => p.PeriodStart >= normalizedStart).ToList();
            }

            if (endDate.HasValue)
            {
                var normalizedEnd = endDate.Value.Date;
                records = records.Where(p => p.PeriodEnd <= normalizedEnd).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                records = records.Where(p => p.Status == parsedStatus).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                records = records.Where(p =>
                        (!string.IsNullOrWhiteSpace(p.PayslipNumber) &&
                         p.PayslipNumber.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Employee?.FullName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.Employee?.Trade?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.Contractor?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.TransactionReference?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            var model = new InvoiceReportViewModel
            {
                SearchTerm = searchTerm?.Trim() ?? string.Empty,
                Invoices = records
                    .OrderByDescending(p => p.PeriodEnd)
                    .ThenByDescending(p => p.Id)
                    .Select(p => new InvoiceReportItem
                    {
                        PayrollId = p.Id,
                        PayslipNumber = p.PayslipNumber ?? $"PS-{p.Id:D6}",
                        WorkerName = p.Employee?.FullName ?? "Unknown Worker",
                        WorkerTrade = p.Employee?.Trade ?? "-",
                        ContractorName = p.Contractor?.Name ?? "-",
                        PeriodStart = p.PeriodStart,
                        PeriodEnd = p.PeriodEnd,
                        DaysWorked = p.DaysWorked,
                        NetPayable = p.NetPayable,
                        Status = p.Status,
                        PaymentDate = p.PaymentDate,
                        PaymentMethod = p.PaymentMethod,
                        TransactionReference = p.TransactionReference
                    })
                    .ToList()
            };

            model.TotalInvoices = model.Invoices.Count;
            model.PaidInvoices = model.Invoices.Count(i => i.Status == PaymentStatus.Paid);
            model.PendingInvoices = model.Invoices.Count(i =>
                i.Status == PaymentStatus.Pending || i.Status == PaymentStatus.PartiallyPaid || i.Status == PaymentStatus.Overdue);
            model.TotalNetAmount = model.Invoices.Sum(i => i.NetPayable);
            model.PaidAmount = model.Invoices
                .Where(i => i.Status == PaymentStatus.Paid)
                .Sum(i => i.NetPayable);
            model.OutstandingAmount = model.Invoices
                .Where(i => i.Status != PaymentStatus.Paid)
                .Sum(i => i.NetPayable);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading invoice report page");
            TempData["Error"] = "Unable to load invoice records right now.";
            return View(new InvoiceReportViewModel());
        }
    }
}
