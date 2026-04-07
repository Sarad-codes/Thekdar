using Thekdar.ViewModels;

namespace Thekdar.Services.Interface
{
    public interface IAuthService
    {
        Task<bool> Register(RegisterViewModel model);
        Task<bool> RegisterEmployee(RegisterViewModel model);
        Task<bool> Login(LoginViewModel model);
        Task Logout();
        Task<UserProfileViewModel> GetCurrentUser();
        bool IsAuthenticated();
        Task<bool> ForgotPassword(ForgotPasswordViewModel model);
        Task<bool> ResetPassword(ResetPasswordViewModel model);
    }
}