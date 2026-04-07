using Thekdar.Data;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Thekdar.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AuthController(
            IAuthService authService,
            ApplicationDbContext context,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.Role = UserRole.Contractor;

                    var result = await _authService.Register(model);
                    if (result)
                        return RedirectToAction("Dashboard", "Home");

                    ModelState.AddModelError(string.Empty, "Unable to register the account right now.");
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            return View(model);
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.Trim().ToLower());

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    if (user.Status == UserStatus.Inactive)
                    {
                        ModelState.AddModelError(string.Empty, "Your account has been deactivated.");
                        return View(model);
                    }

                    if (user.TwoFactorEnabled)
                    {
                        var code = new Random().Next(100000, 999999).ToString();

                        user.PasswordResetToken = code;
                        user.ResetTokenExpires = DateTime.UtcNow.AddMinutes(10);
                        await _context.SaveChangesAsync();

                        await _emailService.SendTwoFactorCodeAsync(user.Email, code, user.Name);

                        HttpContext.Session.SetString("TwoFactorUserId", user.Id.ToString());
                        HttpContext.Session.SetString("TwoFactorRememberMe", model.RememberMe.ToString());
                        HttpContext.Session.SetString("TwoFactorReturnUrl", returnUrl ?? string.Empty);

                        TempData["Info"] = $"A verification code has been sent to {user.Email}";
                        return RedirectToAction("TwoFactor");
                    }

                    await SignInUser(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return View(model);
        }

        public IActionResult TwoFactor(string? returnUrl = null)
        {
            var userId = HttpContext.Session.GetString("TwoFactorUserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            var model = new TwoFactorViewModel
            {
                ReturnUrl = returnUrl ?? HttpContext.Session.GetString("TwoFactorReturnUrl"),
                RememberMe = HttpContext.Session.GetString("TwoFactorRememberMe") == "True"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetString("TwoFactorUserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login");

            if (!int.TryParse(userId, out var parsedUserId))
            {
                HttpContext.Session.Remove("TwoFactorUserId");
                HttpContext.Session.Remove("TwoFactorRememberMe");
                HttpContext.Session.Remove("TwoFactorReturnUrl");
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null || !user.TwoFactorEnabled)
                return RedirectToAction("Login");

            if (user.PasswordResetToken == model.Code && user.ResetTokenExpires > DateTime.UtcNow)
            {
                user.PasswordResetToken = null;
                user.ResetTokenExpires = null;
                await _context.SaveChangesAsync();

                var rememberMe = model.RememberMe;
                var returnUrl = model.ReturnUrl ?? HttpContext.Session.GetString("TwoFactorReturnUrl");

                await SignInUser(user, rememberMe);

                HttpContext.Session.Remove("TwoFactorUserId");
                HttpContext.Session.Remove("TwoFactorRememberMe");
                HttpContext.Session.Remove("TwoFactorReturnUrl");

                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Invalid or expired verification code.");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.Logout();
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _authService.GetCurrentUser();
            if (user == null)
                return Forbid();

            return View(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["Error"] = "Please select an image file.";
                return RedirectToAction("Profile");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var parsedUserId))
                return Forbid();

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
                return NotFound();

            try
            {
                using var memoryStream = new MemoryStream();
                await profilePicture.CopyToAsync(memoryStream);
                user.ProfilePicture = memoryStream.ToArray();
                user.ProfilePictureContentType = profilePicture.ContentType;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile picture updated successfully!";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to update your profile picture right now.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor()
        {
            var user = await _authService.GetCurrentUser();
            if (user == null) return Forbid();

            var fullUser = await _context.Users.FindAsync(user.Id);
            if (fullUser == null) return NotFound();

            try
            {
                fullUser.TwoFactorEnabled = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Two-factor authentication has been enabled. You will be prompted for a code on next login.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to enable two-factor authentication right now.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await _authService.GetCurrentUser();
            if (user == null) return Forbid();

            var fullUser = await _context.Users.FindAsync(user.Id);
            if (fullUser == null) return NotFound();

            try
            {
                fullUser.TwoFactorEnabled = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Two-factor authentication has been disabled.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to disable two-factor authentication right now.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var parsedUserId))
                return Forbid();

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
                return NotFound();

            try
            {
                user.ProfilePicture = null;
                user.ProfilePictureContentType = null;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile picture removed successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Unable to remove your profile picture right now.";
            }

            return RedirectToAction("Profile");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _authService.ForgotPassword(model);
                TempData["Success"] = "If your email exists, you will receive a password reset link.";
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            return View(model);
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.ResetPassword(model);
            if (result)
            {
                TempData["Success"] = "Password reset successfully. Please login with your new password.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, "Invalid or expired reset token.");
            return View(model);
        }

        private async Task SignInUser(UserModel user, bool isPersistent = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard", "Home");
        }
    }
}

