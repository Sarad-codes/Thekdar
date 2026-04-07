using Thekdar.Models;

namespace Thekdar.ViewModels;

public class ReportsViewModel
{
    // Summary Stats
    public int TotalJobs { get; set; }
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int PendingJobs { get; set; }
    
    public int TotalContractors { get; set; }
    public int ActiveContractors { get; set; }
    public int InactiveContractors { get; set; }
    
    public int TotalWorkers { get; set; }
    public int AvailableWorkers { get; set; }
    public int AssignedWorkers { get; set; }
    
    public int TotalAssignments { get; set; }
    public int ActiveAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    
    // Tab Data
    public List<JobReportItem> Jobs { get; set; } = new();
    public List<ContractorReportItem> Contractors { get; set; } = new();
    public List<WorkerReportItem> Workers { get; set; } = new();
    public List<AssignmentReportItem> Assignments { get; set; } = new();
    
    // Search filter
    public string SearchTitle { get; set; } = "";
}

public class JobReportItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ClientName { get; set; }
    public string Address { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public JobStatus Status { get; set; }
    public string CreatedBy { get; set; }
    public string CompletedBy { get; set; }
    public int WorkersAssigned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContractorReportItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int Age { get; set; }
    public int JobsCreated { get; set; }
    public int JobsCompleted { get; set; }
    public int WorkersManaged { get; set; }
    public int ActiveWorkers { get; set; }
    public string Status { get; set; }
    public DateTime JoinedDate { get; set; }
}
public class WorkerReportItem
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Trade { get; set; }
    public string Phone { get; set; }
    public decimal DailyRate { get; set; }  
    public int AssignedJobs { get; set; }
    public int CompletedJobs { get; set; }
    public string ContractorName { get; set; }
    public bool IsAvailable { get; set; }
    public string? ProfilePicturePath { get; set; }
}

public class AssignmentReportItem
{
    public int JobId { get; set; }
    public string JobTitle { get; set; }
    public string ClientName { get; set; }
    public int WorkerId { get; set; }
    public string WorkerName { get; set; }
    public string Trade { get; set; }
    public string Role { get; set; }
    public DateTime AssignedDate { get; set; }
    public string JobStatus { get; set; }
    public string ContractorName { get; set; }
}
