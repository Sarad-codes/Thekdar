using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Thekdar.ViewModels;

public class EmployeeViewModel
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    
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
    public string Phone { get; set; }
    
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be exactly 10 digits")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
    [Display(Name = "Secondary Phone")]
    public string? Phone2 { get; set; }
    
    [EmailAddress]
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    [Required(ErrorMessage = "Daily rate is required")]
    [Display(Name = "Daily Rate (NPR)")]
    [Range(100, 10000, ErrorMessage = "Daily rate must be between NPR 100 and 10,000")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Daily rate must be a whole number (multiples of 100)")]
    [DataType(DataType.Currency)]
    public decimal DailyRate { get; set; }
    
    [Display(Name = "Available for Work")]
    public bool IsAvailable { get; set; } = true;
    
    [Display(Name = "PAN Number")]
    public string? PanNumber { get; set; }
    
    [Display(Name = "PAN Card Image")]
    [DataType(DataType.Upload)]
    public IFormFile? PanCardImage { get; set; }
    
    [Display(Name = "Profile Picture")]
    [DataType(DataType.Upload)]
    public IFormFile? ProfilePicture { get; set; }
    
    public string? ExistingPanCardPath { get; set; }
    public string? ExistingProfilePicturePath { get; set; }
    
    [Display(Name = "Hire Date")]
    [DataType(DataType.Date)]
    public DateTime HireDate { get; set; } = DateTime.Today;
    
    public int? ContractorId { get; set; }
    public string? ContractorName { get; set; }
    
    [Display(Name = "Assign to Jobs")]
    public List<int>? SelectedJobIds { get; set; }
    
    public MultiSelectList? AvailableJobsList { get; set; }
    
    public List<EmployeeJobAssignmentViewModel>? CurrentAssignments { get; set; }

    // ========== MOBILE APP ACCESS PROPERTIES ==========
    
    [Display(Name = "Enable mobile app login")]
    public bool MobileEnabled { get; set; } = false;
    
    // Used to track if mobile was previously enabled (for UI)
    public bool MobileEnabledWas { get; set; } = false;
    
    // Auto-generated password (not stored in DB, only for UI display)
    [Display(Name = "Mobile Password")]
    public string? GeneratedPassword { get; set; }
    
    // Last timeworker logged into mobile app
    [Display(Name = "Last Mobile Login")]
    public string? LastMobileLogin { get; set; }
    
    // Mobile password hash (stored in DB)
    public string? MobilePasswordHash { get; set; }
}