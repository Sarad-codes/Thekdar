using Thekdar.Models;
using Thekdar.Services.Interface;
using Thekdar.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Thekdar.ViewModels;

namespace Thekdar.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<UserModel>> GetAllUsers()
        {
            try
            {
                return await _context.Users
                    .OrderBy(u => u.Name)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsers");
                return new List<UserModel>();
            }
        }

        public async Task<UserModel> GetUserById(int id)
        {
            try
            {
                return await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserById for user {UserId}", id);
                return null;
            }
        }

        public async Task AddUser(UserModel user)
        {
            try
            {
                var normalizedIdentity = await PrepareUniqueUserIdentityAsync(user.Name, user.Email, user.Phone)
                    .ConfigureAwait(false);

                user.Status = UserStatus.Active;
                ApplyBasicUserFields(user, normalizedIdentity.Name, normalizedIdentity.Email, normalizedIdentity.Phone, user.Age);
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync().ConfigureAwait(false);
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
                _logger.LogError(ex, "Error in AddUser for email {Email}", user.Email);
                throw;
            }
        }

        public async Task AddUserFromViewModel(RegisterViewModel model)
        {
            try
            {
                var normalizedIdentity = await PrepareUniqueUserIdentityAsync(model.Name, model.Email, model.Phone)
                    .ConfigureAwait(false);

                var user = new UserModel
                {
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                ApplyBasicUserFields(user, normalizedIdentity.Name, normalizedIdentity.Email, normalizedIdentity.Phone, model.Age);
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync().ConfigureAwait(false);
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
                _logger.LogError(ex, "Error in AddUserFromViewModel for email {Email}", model.Email);
                throw;
            }
        }

        public async Task UpdateUser(UserEditViewModel model)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(model.Id).ConfigureAwait(false);
                if (existingUser == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                var normalizedIdentity = await PrepareUniqueUserIdentityAsync(model.Name, model.Email, model.Phone, model.Id)
                    .ConfigureAwait(false);

                ApplyBasicUserFields(existingUser, normalizedIdentity.Name, normalizedIdentity.Email, normalizedIdentity.Phone, model.Age);
                existingUser.Role = model.Role;
                existingUser.Status = model.Status;

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                }

                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await model.ProfilePicture.CopyToAsync(memoryStream).ConfigureAwait(false);
                    existingUser.ProfilePicture = memoryStream.ToArray();
                    existingUser.ProfilePictureContentType = model.ProfilePicture.ContentType;
                }
                
                await _context.SaveChangesAsync().ConfigureAwait(false);
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
                _logger.LogError(ex, "Error in UpdateUser for user {UserId}", model.Id);
                throw;
            }
        }

        public async Task DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id).ConfigureAwait(false);
                if (user != null)
                {
                    user.Status = UserStatus.Inactive;
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeactivateUser for user {UserId}", id);
                throw;
            }
        }

        public async Task ActivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id).ConfigureAwait(false);
                if (user != null)
                {
                    user.Status = UserStatus.Active;
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ActivateUser for user {UserId}", id);
                throw;
            }
        }

        public async Task<List<UserModel>> GetActiveUsers()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Status == UserStatus.Active)
                    .OrderBy(u => u.Name)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetActiveUsers");
                return new List<UserModel>();
            }
        }

        public async Task<List<UserModel>> GetInactiveUsers()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Status == UserStatus.Inactive)
                    .OrderBy(u => u.Name)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInactiveUsers");
                return new List<UserModel>();
            }
        }

        private static string NormalizeName(string name) => name.Trim();

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private static string NormalizePhone(string phone) => phone.Trim();

        private static void ApplyBasicUserFields(UserModel user, string name, string email, string phone, int age)
        {
            user.Name = name;
            user.Email = email;
            user.Phone = phone;
            user.Age = age;
        }

        private async Task<(string Name, string Email, string Phone)> PrepareUniqueUserIdentityAsync(
            string name,
            string email,
            string phone,
            int? excludingUserId = null)
        {
            var normalizedName = NormalizeName(name);
            var normalizedEmail = NormalizeEmail(email);
            var normalizedPhone = NormalizePhone(phone);

            await EnsureUniqueUserIdentityAsync(normalizedName, normalizedEmail, normalizedPhone, excludingUserId)
                .ConfigureAwait(false);

            return (normalizedName, normalizedEmail, normalizedPhone);
        }

        private async Task EnsureUniqueUserIdentityAsync(string name, string email, string phone, int? excludingUserId = null)
        {
            var nameExists = await _context.Users
                .AnyAsync(u => u.Name.ToLower() == name.ToLower() && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (nameExists)
            {
                throw new InvalidOperationException("An account with this name already exists.");
            }

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == email && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (emailExists)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var phoneExists = await _context.Users
                .AnyAsync(u => u.Phone == phone && (!excludingUserId.HasValue || u.Id != excludingUserId.Value))
                .ConfigureAwait(false);

            if (phoneExists)
            {
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


