using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int Age { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Picture properties
        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureContentType { get; set; }
        
        // 2FA property
        public bool TwoFactorEnabled { get; set; }
    }
}
