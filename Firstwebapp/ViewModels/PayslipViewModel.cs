using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class PayslipViewModel
    {
        // Existing properties...
        public int Id { get; set; }
        public string PayslipNumber { get; set; }
        
        // Employee Details
        public string EmployeeName { get; set; }
        public string EmployeeTrade { get; set; }
        public string EmployeePhone { get; set; }
        public string? EmployeePanNumber { get; set; }
        public string ContractorName { get; set; } = "-";
        
        // Company Details (NEW)
        public string CompanyName { get; set; } = "Devi Pathivara Pvt. Ltd.";
        public string CompanyAddress { get; set; } = "Birtamode, Nepal";
        public string CompanyPhone { get; set; } = "+977-9806027456";
        public string CompanyEmail { get; set; } = "info@Thekdar.com";
        public string CompanyPan { get; set; } = "123456789";
        public string CompanyRegistration { get; set; } = "12345/078/079";
        
        // Period
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string FormattedPeriod => $"{PeriodStart:dd MMM yyyy} - {PeriodEnd:dd MMM yyyy}";
        
        // Earnings
        public int DaysWorked { get; set; }
        public decimal DailyRate { get; set; }
        public decimal BaseWage { get; set; }
        
        // Overtime
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeMultiplier { get; set; }
        public decimal OvertimeWage { get; set; }
        
        // Adjustments
        public decimal Bonus { get; set; }
        public string? BonusReason { get; set; }
        public decimal Deduction { get; set; }
        public string? DeductionReason { get; set; }
        
        // VAT & Contributions (NEW)
        public decimal SsfContribution { get; set; }  // 1% of Base Wage
        public decimal CitContribution { get; set; }  // Fixed amount or percentage
        public decimal VatAmount { get; set; }        // 13% on service
        public decimal TotalDeductions { get; set; }
        
        // Totals
        public decimal TotalWage { get; set; }
        public decimal NetPayable { get; set; }
        
        // Payment
        public PaymentStatus Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionReference { get; set; }
        public string? BankName { get; set; }
        
        public string? Notes { get; set; }
        public DateTime GeneratedDate { get; set; }
        
        // Formatted Display
        public string FormattedDailyRate => $"NPR {DailyRate:N0}";
        public string FormattedBaseWage => $"NPR {BaseWage:N0}";
        public string FormattedOvertimeWage => $"NPR {OvertimeWage:N0}";
        public string FormattedBonus => $"NPR {Bonus:N0}";
        public string FormattedDeduction => $"NPR {Deduction:N0}";
        public string FormattedTotalWage => $"NPR {TotalWage:N0}";
        public string FormattedNetPayable => $"NPR {NetPayable:N0}";
        public string FormattedPaymentDate => PaymentDate?.ToString("dd MMM yyyy") ?? "Not Paid";
        
        // Amount in Words (NEW)
        public string NetPayableInWords => NumberToWords(NetPayable);
        
        private string NumberToWords(decimal number)
        {
            // Simple implementation - you can expand this
            return $"{Math.Floor(number):N0} Rupees Only";
        }
    }
}

