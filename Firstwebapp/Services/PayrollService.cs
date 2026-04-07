using Thekdar.Data;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Thekdar.Services
{
    public class PayrollService : IPayrollService
    {
        private const decimal HoursPerWorkDay = 8m;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PayrollService> _logger;

        public PayrollService(ApplicationDbContext context, ILogger<PayrollService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PayrollWorkSummaryViewModel> GetWorkSummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
        {
            var normalizedStart = periodStart.Date;
            var normalizedEnd = periodEnd.Date;

            if (normalizedEnd < normalizedStart)
            {
                return new PayrollWorkSummaryViewModel();
            }

            var assignments = await _context.JobAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId)
                .Select(a => new
                {
                    a.AssignedDate,
                    a.StartDate,
                    a.EndDate
                })
                .ToListAsync()
                .ConfigureAwait(false);

            var workedDays = new HashSet<DateTime>();
            var matchingAssignments = 0;

            foreach (var assignment in assignments)
            {
                var assignmentStart = (assignment.StartDate ?? assignment.AssignedDate).Date;
                var assignmentEnd = (assignment.EndDate ?? assignment.StartDate ?? assignment.AssignedDate).Date;

                if (assignmentEnd < normalizedStart || assignmentStart > normalizedEnd)
                {
                    continue;
                }

                matchingAssignments++;

                var overlapStart = assignmentStart > normalizedStart ? assignmentStart : normalizedStart;
                var overlapEnd = assignmentEnd < normalizedEnd ? assignmentEnd : normalizedEnd;

                for (var day = overlapStart; day <= overlapEnd; day = day.AddDays(1))
                {
                    workedDays.Add(day);
                }
            }

            return new PayrollWorkSummaryViewModel
            {
                DaysWorked = workedDays.Count,
                MatchingAssignments = matchingAssignments
            };
        }

        public async Task<PayrollModel> SavePayrollRecordAsync(PayrollCalculationViewModel calculation, int contractorId, int createdByUserId)
        {
            ArgumentNullException.ThrowIfNull(calculation);

            if (!calculation.EmployeeId.HasValue)
            {
                throw new InvalidOperationException("Please select a worker before saving payroll.");
            }

            var periodStart = calculation.PeriodStart.Date;
            var periodEnd = calculation.PeriodEnd.Date;
            ValidatePeriod(periodStart, periodEnd);

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == calculation.EmployeeId.Value && !e.IsDeleted)
                .ConfigureAwait(false);

            if (employee == null)
            {
                throw new InvalidOperationException("The selected worker was not found.");
            }

            if (contractorId != employee.ContractorId)
            {
                contractorId = employee.ContractorId;
            }

            ValidateCalculatedValues(calculation, periodStart, periodEnd);
            await EnsureNoDuplicatePayrollAsync(employee.Id, periodStart, periodEnd).ConfigureAwait(false);

            var totals = CalculateTotals(employee.DailyRate, calculation);
            var payroll = new PayrollModel
            {
                EmployeeId = employee.Id,
                ContractorId = contractorId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DaysWorked = calculation.DaysWorked,
                DailyRate = employee.DailyRate,
                BaseWage = totals.BaseWage,
                OvertimeHours = calculation.OvertimeHours,
                OvertimeMultiplier = calculation.OvertimeMultiplier,
                OvertimeWage = totals.OvertimeWage,
                Bonus = calculation.Bonus,
                BonusReason = NormalizeOptionalText(calculation.BonusReason),
                Deduction = calculation.Deduction,
                DeductionReason = NormalizeOptionalText(calculation.DeductionReason),
                TotalWage = totals.TotalWage,
                NetPayable = totals.NetPayable,
                Status = PaymentStatus.Pending,
                Notes = NormalizeOptionalText(calculation.Notes),
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                PayslipGenerated = true,
                PayslipNumber = await GenerateUniquePayslipNumberAsync(periodEnd, employee.Id).ConfigureAwait(false)
            };

            _context.Payrolls.Add(payroll);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Payroll saved for employee {EmployeeId} for {StartDate} to {EndDate} with net payable {NetPayable}",
                payroll.EmployeeId,
                payroll.PeriodStart,
                payroll.PeriodEnd,
                payroll.NetPayable);

            return await GetPayrollByIdAsync(payroll.Id).ConfigureAwait(false) ?? payroll;
        }

        public async Task<List<PayrollModel>> GetAllPayrollRecordsAsync(int? contractorId = null)
        {
            await RefreshOverdueStatusesAsync(contractorId).ConfigureAwait(false);

            return await BuildPayrollQuery(contractorId)
                .OrderByDescending(p => p.PeriodEnd)
                .ThenByDescending(p => p.Id)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<PayrollModel?> GetPayrollByIdAsync(int id)
        {
            await RefreshOverdueStatusesAsync().ConfigureAwait(false);

            return await BuildPayrollQuery()
                .FirstOrDefaultAsync(p => p.Id == id)
                .ConfigureAwait(false);
        }

        public async Task<List<PayrollModel>> GetPayrollByEmployeeAsync(int employeeId)
        {
            await RefreshOverdueStatusesAsync().ConfigureAwait(false);

            return await BuildPayrollQuery()
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PeriodEnd)
                .ThenByDescending(p => p.Id)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<List<PayrollModel>> GetPayrollByDateRangeAsync(DateTime start, DateTime end, int? contractorId = null)
        {
            var normalizedStart = start.Date;
            var normalizedEnd = end.Date;
            ValidatePeriod(normalizedStart, normalizedEnd);

            await RefreshOverdueStatusesAsync(contractorId).ConfigureAwait(false);

            return await BuildPayrollQuery(contractorId)
                .Where(p => p.PeriodEnd >= normalizedStart && p.PeriodStart <= normalizedEnd)
                .OrderByDescending(p => p.PeriodEnd)
                .ThenByDescending(p => p.Id)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task MarkAsPaidAsync(int id, DateTime paymentDate, string paymentMethod, string transactionReference)
        {
            var payroll = await _context.Payrolls.FindAsync(id).ConfigureAwait(false);
            if (payroll == null)
            {
                throw new InvalidOperationException("Payroll record not found.");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new InvalidOperationException("Payment method is required.");
            }

            payroll.Status = PaymentStatus.Paid;
            payroll.PaymentDate = paymentDate.Date;
            payroll.PaymentMethod = paymentMethod.Trim();
            payroll.TransactionReference = NormalizeOptionalText(transactionReference);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Payroll {PayrollId} marked as paid on {PaymentDate}", id, payroll.PaymentDate);
        }

        public async Task UpdatePaymentStatusAsync(int id, PaymentStatus status)
        {
            var payroll = await _context.Payrolls.FindAsync(id).ConfigureAwait(false);
            if (payroll == null)
            {
                throw new InvalidOperationException("Payroll record not found.");
            }

            payroll.Status = status;

            if (status != PaymentStatus.Paid)
            {
                payroll.PaymentDate = null;
                payroll.PaymentMethod = null;
                payroll.TransactionReference = null;
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<PayslipViewModel> GeneratePayslipAsync(int payrollId)
        {
            var payroll = await BuildPayrollQuery()
                .FirstOrDefaultAsync(p => p.Id == payrollId)
                .ConfigureAwait(false);

            if (payroll == null)
            {
                throw new InvalidOperationException("Payroll record not found.");
            }

            var totalDeductions = payroll.Deduction;

            return new PayslipViewModel
            {
                Id = payroll.Id,
                PayslipNumber = payroll.PayslipNumber ?? $"PS-{payroll.Id:D6}",
                EmployeeName = payroll.Employee?.FullName ?? "Unknown Worker",
                EmployeeTrade = payroll.Employee?.Trade ?? "-",
                EmployeePhone = payroll.Employee?.Phone1 ?? "-",
                EmployeePanNumber = payroll.Employee?.PanNumber,
                ContractorName = payroll.Contractor?.Name ?? "-",
                PeriodStart = payroll.PeriodStart,
                PeriodEnd = payroll.PeriodEnd,
                DaysWorked = payroll.DaysWorked,
                DailyRate = payroll.DailyRate,
                BaseWage = payroll.BaseWage,
                OvertimeHours = payroll.OvertimeHours,
                OvertimeMultiplier = payroll.OvertimeMultiplier,
                OvertimeWage = payroll.OvertimeWage,
                Bonus = payroll.Bonus,
                BonusReason = payroll.BonusReason,
                Deduction = payroll.Deduction,
                DeductionReason = payroll.DeductionReason,
                SsfContribution = 0m,
                CitContribution = 0m,
                VatAmount = 0m,
                TotalDeductions = totalDeductions,
                TotalWage = payroll.TotalWage,
                NetPayable = payroll.NetPayable,
                Status = payroll.Status,
                PaymentDate = payroll.PaymentDate,
                PaymentMethod = payroll.PaymentMethod,
                TransactionReference = payroll.TransactionReference,
                Notes = payroll.Notes,
                GeneratedDate = DateTime.Now
            };
        }

        public async Task<PayrollStatisticsViewModel> GetStatisticsAsync(int? contractorId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payrolls.AsNoTracking().AsQueryable();

            if (contractorId.HasValue)
            {
                query = query.Where(p => p.ContractorId == contractorId.Value);
            }

            if (startDate.HasValue)
            {
                var normalizedStart = startDate.Value.Date;
                query = query.Where(p => p.PeriodEnd >= normalizedStart);
            }

            if (endDate.HasValue)
            {
                var normalizedEnd = endDate.Value.Date;
                query = query.Where(p => p.PeriodStart <= normalizedEnd);
            }

            var records = await query.ToListAsync().ConfigureAwait(false);
            var today = DateTime.UtcNow.Date;

            foreach (var record in records.Where(r => r.Status == PaymentStatus.Pending && r.PaymentDate == null && r.PeriodEnd.Date < today.AddDays(-30)))
            {
                record.Status = PaymentStatus.Overdue;
            }

            var groupedOvertime = records
                .GroupBy(p => p.EmployeeId)
                .Select(g => g.Sum(p => p.OvertimeHours))
                .ToList();

            var monthlyGroups = records
                .GroupBy(p => new { p.PeriodEnd.Year, p.PeriodEnd.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .ToList();

            var statistics = new PayrollStatisticsViewModel
            {
                TotalPayments = records.Count,
                TotalPaidAmount = records.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.NetPayable),
                TotalPendingAmount = records.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.NetPayable),
                TotalOverdueAmount = records.Where(p => p.Status == PaymentStatus.Overdue).Sum(p => p.NetPayable),
                PendingCount = records.Count(p => p.Status == PaymentStatus.Pending),
                PaidCount = records.Count(p => p.Status == PaymentStatus.Paid),
                OverdueCount = records.Count(p => p.Status == PaymentStatus.Overdue),
                AverageDailyWage = records.Count != 0 ? Math.Round(records.Average(p => p.DailyRate), 2) : 0m,
                TotalDaysWorked = records.Sum(p => p.DaysWorked),
                TotalOvertimeHours = records.Sum(p => p.OvertimeHours),
                AverageOvertimePerWorker = groupedOvertime.Count != 0 ? Math.Round(groupedOvertime.Average(), 2) : 0m,
                MonthlyTotals = monthlyGroups.ToDictionary(
                    g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    g => g.Sum(p => p.NetPayable)),
                MonthlyPayments = monthlyGroups.ToDictionary(
                    g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    g => g.Count())
            };

            return statistics;
        }

        public async Task DeletePayrollRecordAsync(int id)
        {
            var payroll = await _context.Payrolls.FindAsync(id).ConfigureAwait(false);
            if (payroll == null)
            {
                throw new InvalidOperationException("Payroll record not found.");
            }

            _context.Payrolls.Remove(payroll);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Payroll {PayrollId} deleted", id);
        }

        private IQueryable<PayrollModel> BuildPayrollQuery(int? contractorId = null)
        {
            var query = _context.Payrolls
                .AsNoTracking()
                .Include(p => p.Employee)
                .Include(p => p.Contractor)
                .AsQueryable();

            if (contractorId.HasValue)
            {
                query = query.Where(p => p.ContractorId == contractorId.Value);
            }

            return query;
        }

        private async Task EnsureNoDuplicatePayrollAsync(int employeeId, DateTime periodStart, DateTime periodEnd)
        {
            var exists = await _context.Payrolls
                .AnyAsync(p => p.EmployeeId == employeeId && p.PeriodStart == periodStart && p.PeriodEnd == periodEnd)
                .ConfigureAwait(false);

            if (exists)
            {
                throw new InvalidOperationException("A payroll record for this worker and pay period already exists.");
            }
        }

        private async Task RefreshOverdueStatusesAsync(int? contractorId = null)
        {
            var overdueThreshold = DateTime.UtcNow.Date.AddDays(-30);
            var query = _context.Payrolls
                .Where(p => p.Status == PaymentStatus.Pending && p.PaymentDate == null && p.PeriodEnd < overdueThreshold);

            if (contractorId.HasValue)
            {
                query = query.Where(p => p.ContractorId == contractorId.Value);
            }

            var overdueRecords = await query.ToListAsync().ConfigureAwait(false);
            if (!overdueRecords.Any())
            {
                return;
            }

            foreach (var record in overdueRecords)
            {
                record.Status = PaymentStatus.Overdue;
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task<string> GenerateUniquePayslipNumberAsync(DateTime periodEnd, int employeeId)
        {
            while (true)
            {
                var candidate = $"PS-{periodEnd:yyyyMM}-{employeeId:D4}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
                var exists = await _context.Payrolls
                    .AnyAsync(p => p.PayslipNumber == candidate)
                    .ConfigureAwait(false);

                if (!exists)
                {
                    return candidate;
                }
            }
        }

        private static (decimal BaseWage, decimal OvertimeWage, decimal TotalWage, decimal NetPayable) CalculateTotals(
            decimal dailyRate,
            PayrollCalculationViewModel calculation)
        {
            var baseWage = Math.Round(dailyRate * calculation.DaysWorked, 2, MidpointRounding.AwayFromZero);
            var hourlyRate = dailyRate / HoursPerWorkDay;
            var overtimeWage = Math.Round(hourlyRate * calculation.OvertimeHours * calculation.OvertimeMultiplier, 2, MidpointRounding.AwayFromZero);
            var totalWage = Math.Round(baseWage + overtimeWage + calculation.Bonus, 2, MidpointRounding.AwayFromZero);
            var netPayable = Math.Round(totalWage - calculation.Deduction, 2, MidpointRounding.AwayFromZero);

            if (netPayable < 0)
            {
                throw new InvalidOperationException("Deduction cannot exceed the total earnings for this payroll.");
            }

            return (baseWage, overtimeWage, totalWage, netPayable);
        }

        private static void ValidateCalculatedValues(PayrollCalculationViewModel calculation, DateTime periodStart, DateTime periodEnd)
        {
            if (calculation.DaysWorked < 0)
            {
                throw new InvalidOperationException("Days worked cannot be negative.");
            }

            var totalDaysInPeriod = (periodEnd - periodStart).Days + 1;
            if (calculation.DaysWorked > totalDaysInPeriod)
            {
                throw new InvalidOperationException($"Days worked cannot exceed {totalDaysInPeriod} for the selected period.");
            }

            if (calculation.OvertimeHours < 0)
            {
                throw new InvalidOperationException("Overtime hours cannot be negative.");
            }

            if (calculation.OvertimeMultiplier < 1)
            {
                throw new InvalidOperationException("Overtime multiplier must be at least 1.");
            }
        }

        private static void ValidatePeriod(DateTime periodStart, DateTime periodEnd)
        {
            if (periodEnd < periodStart)
            {
                throw new InvalidOperationException("Pay period end cannot be earlier than the start date.");
            }
        }

        private static string? NormalizeOptionalText(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
