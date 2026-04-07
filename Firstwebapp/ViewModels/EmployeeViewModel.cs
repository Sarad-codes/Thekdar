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
    [DataType(DataType.Currency)]
    public decimal DailyRate { get; set; }  // Changed from HourlyRate to DailyRate
    
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
}
