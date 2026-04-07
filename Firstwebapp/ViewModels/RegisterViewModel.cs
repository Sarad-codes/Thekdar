using System.ComponentModel.DataAnnotations;
using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be 10 digits")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be 10 digits")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Age is required")]
        [Range(18, 100, ErrorMessage = "Age must be between 18 and 100")]
        public int Age { get; set; }

        [Display(Name = "Register As")]
        public UserRole Role { get; set; } = UserRole.Contractor;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Password must be at least 2 characters")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
        
        [Display(Name = "Profile Picture")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfilePicture { get; set; }
    }
}