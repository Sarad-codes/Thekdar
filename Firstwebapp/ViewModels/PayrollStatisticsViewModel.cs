namespace Thekdar.ViewModels
{
    public class PayrollStatisticsViewModel
    {
        // Summary
        public int TotalPayments { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalOverdueAmount { get; set; }
        
        // Counts
        public int PendingCount { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        
        // Averages
        public decimal AverageDailyWage { get; set; }
        public int TotalDaysWorked { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public decimal AverageOvertimePerWorker { get; set; }
        
        // Monthly Breakdown (Optional)
        public Dictionary<string, decimal> MonthlyTotals { get; set; } = new();
        public Dictionary<string, int> MonthlyPayments { get; set; } = new();
        
        // Formatted
        public string FormattedTotalPaid => $"NPR {TotalPaidAmount:N0}";
        public string FormattedTotalPending => $"NPR {TotalPendingAmount:N0}";
        public string FormattedTotalOverdue => $"NPR {TotalOverdueAmount:N0}";
        public string FormattedAverageDailyWage => $"NPR {AverageDailyWage:N0}";
    }
}