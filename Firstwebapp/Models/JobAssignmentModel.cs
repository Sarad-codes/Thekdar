using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thekdar.Models;

public class JobAssignmentModel
{
    [Key]
    public int Id { get; set; }

    public int JobId { get; set; }
    public int EmployeeId { get; set; }
    public int? AssignedByUserId { get; set; }

    [Display(Name = "Assigned Date")]
    public DateTime AssignedDate { get; set; }

    [Display(Name = "Assigned At")]
    public DateTime AssignedAt { get; set; }

    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Hours Worked")]
    public decimal HoursWorked { get; set; }

    [Display(Name = "Role")]
    public string Role { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; }

    [ForeignKey(nameof(JobId))]
    public JobModel Job { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public EmployeeModel? Employee { get; set; }

    [ForeignKey(nameof(AssignedByUserId))]
    public UserModel? AssignedByUser { get; set; }
}

public class JobAssignmentOperationResult
{
    public List<string> Warnings { get; set; } = new();
    public List<int> AssignedEmployeeIds { get; set; } = new();
}

