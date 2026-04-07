using System.ComponentModel.DataAnnotations;

namespace Thekdar.ViewModels
{
    public class PayrollCalculationModel
    {
        // Employee Selection
        [Required(ErrorMessage = "Please select an employee")]
        public int EmployeeId { get; set; }
        
        // Period Selection
        [Required(ErrorMessage = "Pay period start is required")]
        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; }
        
        [Required(ErrorMessage = "Pay period end is required")]
        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; }
        
        // Work Details
        [Range(0, 31, ErrorMessage = "Days must be between 0 and 31")]
        public int DaysWorked { get; set; }
        
        [Range(0, 240, ErrorMessage = "Overtime hours must be between 0 and 240")]
        public decimal OvertimeHours { get; set; }
        
        [Range(1, 3, ErrorMessage = "Overtime multiplier must be between 1 and 3")]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;
        
        // Adjustments
        [Range(0, 100000, ErrorMessage = "Bonus must be between 0 and 100,000")]
        public decimal Bonus { get; set; }
        
        public string? BonusReason { get; set; }
        
        [Range(0, 100000, ErrorMessage = "Deduction must be between 0 and 100,000")]
        public decimal Deduction { get; set; }
        
        public string? DeductionReason { get; set; }
        
        public string? Notes { get; set; }
    }
}
