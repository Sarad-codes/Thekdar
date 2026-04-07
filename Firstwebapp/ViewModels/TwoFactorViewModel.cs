using System.ComponentModel.DataAnnotations;

namespace Thekdar.ViewModels;

public class TwoFactorViewModel
{
    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
    [Display(Name = "Verification Code")]
    public string Code { get; set; } = string.Empty;
    
    public string? ReturnUrl { get; set; }
    public bool RememberMe { get; set; }
}