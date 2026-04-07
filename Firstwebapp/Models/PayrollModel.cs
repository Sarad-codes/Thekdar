using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thekdar.Models
{
    public enum PaymentStatus
    {
        Pending,
        Paid,
        PartiallyPaid,
        Overdue
    }

    public class PayrollModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int EmployeeId { get; set; }
        
        [ForeignKey("EmployeeId")]
        public virtual EmployeeModel? Employee { get; set; }
        
        [Required]
        public int ContractorId { get; set; }
        
        [ForeignKey("ContractorId")]
        public virtual UserModel? Contractor { get; set; }
        
        [Required]
        [Display(Name = "Pay Period Start")]
        [DataType(DataType.Date)]
        public DateTime PeriodStart { get; set; }
        
        [Required]
        [Display(Name = "Pay Period End")]
        [DataType(DataType.Date)]
        public DateTime PeriodEnd { get; set; }
        
        [Display(Name = "Days Worked")]
        [Range(0, 31)]
        public int DaysWorked { get; set; }
        
        [Display(Name = "Daily Rate (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyRate { get; set; }
        
        [Display(Name = "Base Wage (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseWage { get; set; }
        
        [Display(Name = "Overtime Hours")]
        [Range(0, 240)]
        public decimal OvertimeHours { get; set; }
        
        [Display(Name = "Overtime Multiplier")]
        public decimal OvertimeMultiplier { get; set; } = 1.5m;
        
        [Display(Name = "Overtime Wage (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeWage { get; set; }
        
        [Display(Name = "Bonus (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Bonus { get; set; }
        
        [Display(Name = "Bonus Reason")]
        public string? BonusReason { get; set; }
        
        [Display(Name = "Deduction (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Deduction { get; set; }
        
        [Display(Name = "Deduction Reason")]
        public string? DeductionReason { get; set; }
        
        [Display(Name = "Total Wage (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWage { get; set; }
        
        [Display(Name = "Net Payable (NPR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetPayable { get; set; }
        
        [Display(Name = "Payment Status")]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        
        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }
        
        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }
        
        [Display(Name = "Transaction Reference")]
        public string? TransactionReference { get; set; }
        
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
        
        public int CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool PayslipGenerated { get; set; } = true;
        
        public string? PayslipNumber { get; set; }
        
        // For invoice integration (coming soon)
        public int? InvoiceId { get; set; }
        
        [NotMapped]
        public string FormattedPeriod => $"{PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}";
        
        [NotMapped]
        public string FormattedNetPayable => $"NPR {NetPayable:N0}";
    }
}
