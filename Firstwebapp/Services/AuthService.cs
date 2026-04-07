using Thekdar.Models;
using Thekdar.Data;
using Thekdar.ViewModels;
using Thekdar.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using System.Security.Cryptography;
using BCrypt.Net;

namespace Thekdar.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;

        public AuthService(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor, 
            ILogger<AuthService> logger,
            IEmailService emailService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _emailService = emailService;
        }

        // Public register â€” always creates a Contractor
        public async Task<bool> Register(RegisterViewModel model)
        {
            try
            {
                var normalizedName = NormalizeName(model.Name);
                var normalizedEmail = NormalizeEmail(model.Email);
                var normalizedPhone = NormalizePhone(model.Phone);
                await EnsureUniqueUserIdentityAsync(normalizedName, normalizedEmail, normalizedPhone).ConfigureAwait(false);

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Handle profile picture
                byte[]? profilePicture = null;
                string? profilePictureContentType = null;
                
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.ProfilePicture.CopyToAsync(memoryStream).ConfigureAwait(false);
                        profilePicture = memoryStream.ToArray();
                        profilePictureContentType = model.ProfilePicture.ContentType;
                    }
                }

                var user = new UserModel
                {
                    Name = normalizedName,
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    Age = model.Age,
                    Role = UserRole.Contractor,
                    Status = UserStatus.Active,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    ProfilePicture = profilePicture,
                    ProfilePictureContentType = profilePictureContentType
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                await SignInUser(user).ConfigureAwait(false);
                _logger.LogInformation($"New user registered: {user.Email}");
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex) when (TryTranslateUserWriteException(ex, out var message))
            {
                throw new InvalidOperationException(message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return false;
            }
        }

        // Admin adds employee â€” no auto-login, role is whatever Admin picked
        public async Task<bool> RegisterEmployee(RegisterViewModel model)
        {
            try
            {
                var normalizedName = NormalizeName(model.Name);
                var normalizedEmail = NormalizeEmail(model.Email);
                var normalizedPhone = NormalizePhone(model.Phone);
                await EnsureUniqueUserIdentityAsync(normalizedName, normalizedEmail, normalizedPhone).ConfigureAwait(false);

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Handle profile picture for admin-created users
                byte[]? profilePicture = null;
                string? profilePictureContentType = null;
                
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.ProfilePicture.CopyToAsync(memoryStream).ConfigureAwait(false);
                        profilePicture = memoryStream.ToArray();
                        profilePictureContentType = model.ProfilePicture.ContentType;
                    }
                }

                var user = new UserModel
                {
                    Name = normalizedName,
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    Age = model.Age,
                    Role = model.Role == UserRole.Admin ? UserRole.Contractor : model.Role,
                    Status = UserStatus.Active,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow,
                    ProfilePicture = profilePicture,
                    ProfilePictureContentType = profilePictureContentType
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation($"Admin created new user: {user.Email} with role {user.Role}");
                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex) when (TryTranslateUserWriteException(ex, out var message))
            {
                throw new InvalidOperationException(message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin user creation");
                return false;
            }
        }

        public async Task<bool> Login(LoginViewModel model)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.Trim().ToLower())
                    .ConfigureAwait(false);

                if (user == null)
                {
                    _logger.LogWarning($"Login failed - user not found: {model.Email}");
                    return false;
                }
                
                if (user.Status == UserStatus.Inactive)
                {
                    _logger.LogWarning($"Login attempt for inactive user: {user.Email}");
                    return false;
                }

                bool validPassword = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                if (!validPassword)
                {
                    _logger.LogWarning($"Invalid password for user: {user.Email}");
                    return false;
                }

                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync().ConfigureAwait(false);

                await SignInUser(user, model.RememberMe).ConfigureAwait(false);
                _logger.LogInformation($"User logged in: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return false;
            }
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

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogError("HttpContext is null during sign-in");
                return;
            }

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties).ConfigureAwait(false);
        }

        public async Task Logout()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
                _logger.LogInformation("User logged out");
            }
        }

        public async Task<UserProfileViewModel> GetCurrentUser()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetCurrentUser called with no authenticated user");
                return null;
            }

            var user = await _context.Users
                .FindAsync(int.Parse(userId))
                .ConfigureAwait(false);
                
            if (user == null)
            {
                _logger.LogWarning($"User not found for ID: {userId}");
                return null;
            }

            return new UserProfileViewModel
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Age = user.Age,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                ProfilePicture = user.ProfilePicture,
                ProfilePictureContentType = user.ProfilePictureContentType,
                TwoFactorEnabled = user.TwoFactorEnabled  // Added this line
            };
        }

        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public async Task<bool> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.Trim().ToLower())
                    .ConfigureAwait(false);
                    
                if (user == null)
                {
                    // Return true to prevent email enumeration
                    _logger.LogInformation($"Password reset requested for non-existent email: {model.Email}");
                    return true;
                }

                // Rate limiting - check if last reset was within 5 minutes
                if (user.ResetTokenExpires.HasValue && 
                    user.ResetTokenExpires.Value > DateTime.UtcNow.AddMinutes(-5))
                {
                    _logger.LogWarning($"Rate limit hit for password reset: {user.Email}");
                    return true;
                }

                user.PasswordResetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                user.ResetTokenExpires = DateTime.UtcNow.AddHours(24);
                await _context.SaveChangesAsync().ConfigureAwait(false);

                var request = _httpContextAccessor.HttpContext?.Request;
                if (request != null)
                {
                    var baseUrl = $"{request.Scheme}://{request.Host}";
                    var resetLink = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(user.PasswordResetToken)}&email={Uri.EscapeDataString(user.Email)}";
                    
                    // Send email
                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, user.Name);
                    _logger.LogInformation($"Password reset email sent to {user.Email}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password process");
                return false;
            }
        }

        public async Task<bool> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.Trim().ToLower())
                    .ConfigureAwait(false);
                
                if (user == null)
                {
                    _logger.LogWarning($"Password reset attempted for non-existent user: {model.Email}");
                    return false;
                }
                
                if (string.IsNullOrEmpty(user.PasswordResetToken) || user.PasswordResetToken != model.Token)
                {
                    _logger.LogWarning($"Invalid reset token for user: {model.Email}");
                    return false;
                }
                
                if (user.ResetTokenExpires < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Expired reset token for user: {model.Email}");
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                user.PasswordResetToken = null;
                user.ResetTokenExpires = null;
                await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation($"Password reset successfully for user: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return false;
            }
        }

        private static string NormalizeName(string name) => name.Trim();

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static string NormalizePhone(string phone) => phone.Trim();

        private async Task EnsureUniqueUserIdentityAsync(string name, string email, string phone, int? excludingUserId = null)
        {
            var nameExists = await _context.Users
                .AnyAsync(u => u.Name.ToLower() == name.ToLower() && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (nameExists)
            {
                _logger.LogWarning("Duplicate user name rejected: {Name}", name);
                throw new InvalidOperationException("An account with this name already exists.");
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == email && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (emailExists)
            {
                _logger.LogWarning("Duplicate user email rejected: {Email}", email);
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == phone && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (phoneExists)
            {
                _logger.LogWarning("Duplicate user phone rejected: {Phone}", phone);
                throw new InvalidOperationException("An account with this phone number already exists.");
            }
        }

        private static bool TryTranslateUserWriteException(DbUpdateException ex, out string message)
        {
            if (ex.InnerException is PostgresException postgresException)
            {
                message = postgresException.ConstraintName switch
                {
                    "IX_Users_Name" => "An account with this name already exists.",
                    "IX_Users_Email" => "An account with this email already exists.",
                    "IX_Users_Phone" => "An account with this phone number already exists.",
                    _ => string.Empty
                };

                return !string.IsNullOrWhiteSpace(message);
            }

            message = string.Empty;
            return false;
        }
    }
}

