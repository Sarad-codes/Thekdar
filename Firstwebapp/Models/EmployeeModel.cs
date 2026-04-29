using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Thekdar.Models;

public class EmployeeModel
{
    [Key]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }
    
    [Required(ErrorMessage = "Last name is required")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    [Required(ErrorMessage = "Trade is required")]
    [Display(Name = "Trade/Skill")]
    public string Trade { get; set; }
    
    [Required(ErrorMessage = "Primary phone is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be exactly 10 digits")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
    [Display(Name = "Primary Phone")]
    public string Phone1 { get; set; }
    
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be exactly 10 digits")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
    [Display(Name = "Secondary Phone")]
    public string? Phone2 { get; set; }
    
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Required(ErrorMessage = "Daily rate is required")]
    [Display(Name = "Daily Rate (NPR)")]
    [Column(TypeName = "decimal(18,2)")]
    [Range(100, 10000, ErrorMessage = "Daily rate must be between NPR 100 and 10,000")]
    public decimal DailyRate { get; set; }
    
    [Display(Name = "Available for Work")]
    public bool IsAvailable { get; set; } = true;
    
    [Display(Name = "PAN Number")]
    public string? PanNumber { get; set; }
    
    [Display(Name = "PAN Card Image")]
    public string? PanCardImagePath { get; set; }
    
    [Display(Name = "Profile Picture")]
    public string? ProfilePicturePath { get; set; }
    
    [Display(Name = "Hire Date")]
    public DateTime HireDate { get; set; }
    
    [Display(Name = "Deleted")]
    public bool IsDeleted { get; set; } = false;
    
    // ========== MOBILE APP ACCESS PROPERTIES ==========
    
    [Display(Name = "Enable mobile app login")]
    public bool MobileEnabled { get; set; } = false;
    
    [Display(Name = "Mobile Password Hash")]
    public string? MobilePasswordHash { get; set; }
    
    [Display(Name = "Last Mobile Login")]
    public DateTime? LastMobileLogin { get; set; }
    
    // ========== END MOBILE APP ACCESS PROPERTIES ==========
    
    public int ContractorId { get; set; }
    
    [ForeignKey("ContractorId")]
    public UserModel Contractor { get; set; }
 
    public ICollection<JobAssignmentModel> JobAssignments { get; set; }
}