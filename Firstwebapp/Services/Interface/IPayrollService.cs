using Thekdar.Models;
using Thekdar.ViewModels;

namespace Thekdar.Services.Interface
{
    public interface IPayrollService
    {
        Task<PayrollWorkSummaryViewModel> GetWorkSummaryAsync(int employeeId, DateTime periodStart, DateTime periodEnd);
        Task<PayrollModel> SavePayrollRecordAsync(PayrollCalculationViewModel calculation, int contractorId, int createdByUserId);
        Task<List<PayrollModel>> GetAllPayrollRecordsAsync(int? contractorId = null);
        Task<PayrollModel?> GetPayrollByIdAsync(int id);
        Task<List<PayrollModel>> GetPayrollByEmployeeAsync(int employeeId);
        Task<List<PayrollModel>> GetPayrollByDateRangeAsync(DateTime start, DateTime end, int? contractorId = null);
        Task MarkAsPaidAsync(int id, DateTime paymentDate, string paymentMethod, string transactionReference);
        Task UpdatePaymentStatusAsync(int id, PaymentStatus status);
        Task<PayslipViewModel> GeneratePayslipAsync(int payrollId);
        Task<PayrollStatisticsViewModel> GetStatisticsAsync(int? contractorId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task DeletePayrollRecordAsync(int id);
    }
}
