using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Thekdar.Models;

public class MobileAttendance
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int WorkerId { get; set; }

    [Required]
    public int JobId { get; set; }

    [Required]
    public DateTime ClockInTime { get; set; }

    public DateTime? ClockOutTime { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal? OutLatitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? OutLongitude { get; set; }

    public string? PhotoPath { get; set; }

    public string? OutPhotoPath { get; set; }

    public string? Notes { get; set; }

    public bool IsSynced { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("WorkerId")]
    public EmployeeModel Worker { get; set; }

    [ForeignKey("JobId")]
    public JobModel Job { get; set; }
}