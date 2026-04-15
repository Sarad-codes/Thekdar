using System.ComponentModel.DataAnnotations;

namespace Thekdar.Models;

public enum UserStatus { Active, Inactive }
public enum UserRole { Admin, Contractor }

public class UserModel
{
    public int Id { get; set; }

    [Required] public string Name { get; set; }
    [Required][EmailAddress] public string Email { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 10)]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
    public string Phone { get; set; }

    [Required][Range(0, 100)] public int Age { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    public UserRole Role { get; set; } = UserRole.Contractor;

    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }
    
    public byte[]? ProfilePicture { get; set; }       
    public string? ProfilePictureContentType { get; set; } 

    // 2FA - Simple email-based two-factor authentication
    public bool TwoFactorEnabled { get; set; } = false;

    // Navigation  jobs this user created or completed
    public ICollection<JobModel> JobsCreated { get; set; } = new List<JobModel>();
    public ICollection<JobModel> JobsCompleted { get; set; } = new List<JobModel>();
    
    // Navigation  employees this contractor manages
    public ICollection<EmployeeModel> Employees { get; set; } = new List<EmployeeModel>();
}
