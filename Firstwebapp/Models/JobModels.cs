using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thekdar.Models;

public enum JobStatus
{
    Active,
    PendingConfirmation,
    Completed
}

public class JobModel
{
    [Key]
    public int Id { get; set; }  // Changed to int

    [Required(ErrorMessage = "Title is required")]
    [StringLength(150)]
    public string Title { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Client Name")]
    public string? ClientName { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Scheduled Date")]
    public DateTime? ScheduledDate { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Active;

    public bool IsCompleted => Status == JobStatus.Completed;

    // Who created this job
    public int? CreatedByUserId { get; set; }

    [ForeignKey("CreatedByUserId")]
    public UserModel? CreatedBy { get; set; }

    // Who marked/confirmed it complete
    public int? CompletedByUserId { get; set; }

    [ForeignKey("CompletedByUserId")]
    public UserModel? CompletedBy { get; set; }

    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; }
}
