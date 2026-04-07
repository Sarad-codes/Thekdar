using System.ComponentModel.DataAnnotations;
using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class UserEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone must be exactly 10 digits")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Age is required")]
        [Range(0, 100, ErrorMessage = "Age must be between 0 and 100")]
        public int Age { get; set; }

        public UserRole Role { get; set; }

        public UserStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool HasProfilePicture { get; set; }

        [Display(Name = "Profile Picture")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfilePicture { get; set; }

        [Display(Name = "New Password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "New password must be at least 2 characters")]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string? ConfirmNewPassword { get; set; }
    }
}
