using System.ComponentModel.DataAnnotations;

namespace Thekdar.ViewModels
{
    public class PayrollCalculationViewModel
    {
        // Employee Selection
        [Required(ErrorMessage = "Please select an employee")]
        [Display(Name = "Select Worker")]
        public int? EmployeeId { get; set; }
        
        public string? EmployeeName { get; set; }
        public string? EmployeeTrade { get; set; }
        public decimal DailyRate { get; set; }
        
        // Period Selection
        [Required(ErrorMessage = "Pay period start is required")]
        [Display(Name = "Period Start")]
        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; } = DateTime.Now.AddDays(-DateTime.Now.Day + 1);
        
        [Required(ErrorMessage = "Pay period end is required")]
        [Display(Name = "Period End")]
        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; } = DateTime.Now;
        
        // Work Details
        [Display(Name = "Days Worked")]
        [Range(0, 366, ErrorMessage = "Days must be between 0 and 366")]
        public int DaysWorked { get; set; } = 0;
        
        [Display(Name = "Overtime Hours")]
        [Range(0, 240, ErrorMessage = "Overtime hours must be between 0 and 240")]
        public decimal OvertimeHours { get; set; } = 0;
        
        [Display(Name = "Overtime Rate")]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;
        
        [Display(Name = "Bonus (NPR)")]
        [Range(0, 100000, ErrorMessage = "Bonus must be between 0 and 100,000")]
        public decimal Bonus { get; set; } = 0;
        
        [Display(Name = "Bonus Reason")]
        public string? BonusReason { get; set; }
        
        [Display(Name = "Deduction (NPR)")]
        [Range(0, 100000, ErrorMessage = "Deduction must be between 0 and 100,000")]
        public decimal Deduction { get; set; } = 0;
        
        [Display(Name = "Deduction Reason")]
        public string? DeductionReason { get; set; }
        
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
        
        // Calculated Fields (Auto-populated)
        public decimal BaseWage { get; set; }
        public decimal OvertimeWage { get; set; }
        public decimal TotalWage { get; set; }
        public decimal NetPayable { get; set; }

        public int SuggestedDaysWorked { get; set; }
        public int MatchingAssignments { get; set; }
        public bool HasCalculated { get; set; }
        
        // Search
        public string? SearchTerm { get; set; }
        public List<EmployeeSearchResult>? SearchResults { get; set; }
    }
    
    public class EmployeeSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Trade { get; set; } = string.Empty;
        public decimal DailyRate { get; set; }
        public string Phone { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }

    public class PayrollWorkSummaryViewModel
    {
        public int DaysWorked { get; set; }
        public int MatchingAssignments { get; set; }
    }
}
