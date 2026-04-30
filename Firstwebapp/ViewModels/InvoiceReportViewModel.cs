using Thekdar.Models;

namespace Thekdar.ViewModels;

public class InvoiceReportViewModel
{
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public decimal TotalNetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public List<InvoiceReportItem> Invoices { get; set; } = new();
}

public class InvoiceReportItem
{
    public int PayrollId { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public string WorkerTrade { get; set; } = string.Empty;
    public string ContractorName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int DaysWorked { get; set; }
    public decimal NetPayable { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    public string PeriodLabel => $"{PeriodStart:dd MMM yyyy} - {PeriodEnd:dd MMM yyyy}";
}
