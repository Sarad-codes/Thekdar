using Thekdar.Data;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Thekdar.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<EmployeeService> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<List<EmployeeModel>> GetAllAsync()
        {
            try
            {
                return await _context.Employees
                    .Where(e => !e.IsDeleted)
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employees");
                return new List<EmployeeModel>();
            }
        }

        public async Task<EmployeeModel?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Employees
                    .IgnoreQueryFilters()
                    .Include(e => e.Contractor)
                    .FirstOrDefaultAsync(e => e.Id == id)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting employee by ID: {id}");
                return null;
            }
        }

        public async Task<EmployeeModel> CreateAsync(EmployeeViewModel model, int contractorId)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                if (contractorId == 0)
                    throw new ArgumentException("Contractor ID cannot be empty", nameof(contractorId));

                // Validate required fields
                if (string.IsNullOrWhiteSpace(model.FirstName))
                    throw new ArgumentException("First name is required");
                if (string.IsNullOrWhiteSpace(model.LastName))
                    throw new ArgumentException("Last name is required");
                if (string.IsNullOrWhiteSpace(model.Trade))
                    throw new ArgumentException("Trade is required");
                if (model.DailyRate <= 0)
                    throw new ArgumentException("Daily rate must be greater than 0");
                
                // Validate primary phone (required)
                if (string.IsNullOrWhiteSpace(model.Phone))
                    throw new ArgumentException("Primary phone is required");
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^\d{10}$"))
                    throw new ArgumentException("Primary phone must be exactly 10 digits");

                // Validate secondary phone ONLY if provided (optional)
                if (!string.IsNullOrEmpty(model.Phone2) && !System.Text.RegularExpressions.Regex.IsMatch(model.Phone2, @"^\d{10}$"))
                    throw new ArgumentException("Secondary phone must be exactly 10 digits");

                var normalizedPhone1 = NormalizePhone(model.Phone);
                var normalizedPhone2 = NormalizeOptionalPhone(model.Phone2);
                var normalizedEmail = NormalizeOptionalEmail(model.Email);
                var normalizedPanNumber = NormalizeOptionalText(model.PanNumber);

                await EnsureEmployeeContactUniquenessAsync(
                        employeeId: null,
                        firstName: model.FirstName.Trim(),
                        lastName: model.LastName.Trim(),
                        phone1: normalizedPhone1,
                        phone2: normalizedPhone2,
                        email: normalizedEmail,
                        panNumber: normalizedPanNumber)
                    .ConfigureAwait(false);

                // Hire date validation
                DateTime hireDateUtc;
                try
                {
                    hireDateUtc = DateTime.SpecifyKind(model.HireDate, DateTimeKind.Utc);
                }
                catch
                {
                    hireDateUtc = DateTime.UtcNow;
                }
                
                // Handle image uploads (optional)
                string panPath = null;
                string profilePath = null;

                if (model.PanCardImage != null && model.PanCardImage.Length > 0)
                {
                    panPath = await SaveImage(model.PanCardImage, "pan", contractorId).ConfigureAwait(false);
                }

                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    profilePath = await SaveImage(model.ProfilePicture, "profile", contractorId).ConfigureAwait(false);
                }

                var employee = new EmployeeModel
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Trade = model.Trade.Trim(),
                    Phone1 = normalizedPhone1,
                    Phone2 = normalizedPhone2,
                    Email = normalizedEmail,
                    DailyRate = model.DailyRate,
                    IsAvailable = model.IsAvailable,
                    PanNumber = normalizedPanNumber,
                    PanCardImagePath = panPath,
                    ProfilePicturePath = profilePath,
                    HireDate = hireDateUtc,
                    ContractorId = contractorId,
                    IsDeleted = false
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync().ConfigureAwait(false);
                
                _logger.LogInformation($"Employee created: {employee.Id} - {employee.FirstName} {employee.LastName} by contractor {contractorId} with daily rate NPR {employee.DailyRate}");
                return employee;
            }
            catch (DbUpdateException ex) when (TryTranslateEmployeeWriteException(ex, out var message))
            {
                _logger.LogWarning(ex, "Employee creation blocked by constraint");
                throw new InvalidOperationException(message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating employee: {model?.FirstName} {model?.LastName}");
                throw;
            }
        }

        public async Task UpdateAsync(EmployeeViewModel model)
        {
            try
            {
                var existing = await _context.Employees
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(e => e.Id == model.Id)
                    .ConfigureAwait(false);
                    
                if (existing == null)
                {
                    _logger.LogWarning($"Employee not found for update: {model.Id}");
                    throw new InvalidOperationException("Employee not found.");
                }

                // Validate primary phone (required)
                if (string.IsNullOrWhiteSpace(model.Phone))
                    throw new ArgumentException("Primary phone is required");
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^\d{10}$"))
                    throw new ArgumentException("Primary phone must be exactly 10 digits");

                // Validate secondary phone ONLY if provided (optional)
                if (!string.IsNullOrEmpty(model.Phone2) && !System.Text.RegularExpressions.Regex.IsMatch(model.Phone2, @"^\d{10}$"))
                    throw new ArgumentException("Secondary phone must be exactly 10 digits");

                var normalizedPhone1 = NormalizePhone(model.Phone);
                var normalizedPhone2 = NormalizeOptionalPhone(model.Phone2);
                var normalizedEmail = NormalizeOptionalEmail(model.Email);
                var normalizedPanNumber = NormalizeOptionalText(model.PanNumber);

                await EnsureEmployeeContactUniquenessAsync(
                        employeeId: model.Id,
                        firstName: model.FirstName.Trim(),
                        lastName: model.LastName.Trim(),
                        phone1: normalizedPhone1,
                        phone2: normalizedPhone2,
                        email: normalizedEmail,
                        panNumber: normalizedPanNumber)
                    .ConfigureAwait(false);

                DateTime hireDateUtc;
                try
                {
                    hireDateUtc = DateTime.SpecifyKind(model.HireDate, DateTimeKind.Utc);
                }
                catch
                {
                    hireDateUtc = DateTime.UtcNow;
                }

                // Handle image updates
                if (model.PanCardImage != null && model.PanCardImage.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.PanCardImagePath))
                    {
                        DeleteImage(existing.PanCardImagePath);
                    }
                    existing.PanCardImagePath = await SaveImage(model.PanCardImage, "pan", existing.ContractorId).ConfigureAwait(false);
                }

                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existing.ProfilePicturePath))
                    {
                        DeleteImage(existing.ProfilePicturePath);
                    }
                    existing.ProfilePicturePath = await SaveImage(model.ProfilePicture, "profile", existing.ContractorId).ConfigureAwait(false);
                }

                existing.FirstName = model.FirstName.Trim();
                existing.LastName = model.LastName.Trim();
                existing.Trade = model.Trade.Trim();
                existing.Phone1 = normalizedPhone1;
                existing.Phone2 = normalizedPhone2;
                existing.Email = normalizedEmail;
                existing.DailyRate = model.DailyRate;
                existing.IsAvailable = existing.IsDeleted ? false : model.IsAvailable;
                existing.PanNumber = normalizedPanNumber;
                existing.HireDate = hireDateUtc;

                await _context.SaveChangesAsync().ConfigureAwait(false);
                _logger.LogInformation($"Employee updated: {model.Id} with daily rate NPR {existing.DailyRate}");
            }
            catch (DbUpdateException ex) when (TryTranslateEmployeeWriteException(ex, out var message))
            {
                _logger.LogWarning(ex, "Employee update blocked by constraint");
                throw new InvalidOperationException(message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating employee: {model?.Id}");
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning($"Employee not found for soft delete: {id}");
                    return;
                }
        
                if (employee.IsDeleted)
                {
                    _logger.LogWarning($"Employee already deleted: {id}");
                    return;
                }

                // Get and remove all active assignments
                var activeAssignments = await _context.JobAssignments
                    .Where(a => a.EmployeeId == id && a.Status != "Completed")
                    .ToListAsync()
                    .ConfigureAwait(false);
        
                if (activeAssignments.Any())
                {
                    _context.JobAssignments.RemoveRange(activeAssignments);
                    _logger.LogInformation($"Removed {activeAssignments.Count} active assignments for employee {id}");
                }

                employee.IsDeleted = true;
                employee.IsAvailable = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Employee soft deleted: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting employee: {id}");
                throw;
            }
        }

        public async Task ReactivateAsync(int id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.LogWarning($"Employee not found for reactivation: {id}");
                    return;
                }
                
                if (!employee.IsDeleted)
                {
                    _logger.LogWarning($"Employee already active: {id}");
                    return;
                }

                await EnsureEmployeeContactUniquenessAsync(
                        employeeId: employee.Id,
                        firstName: employee.FirstName,
                        lastName: employee.LastName,
                        phone1: employee.Phone1,
                        phone2: employee.Phone2,
                        email: employee.Email,
                        panNumber: employee.PanNumber)
                    .ConfigureAwait(false);

                employee.IsDeleted = false;
                employee.IsAvailable = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Employee reactivated: {id}");
            }
            catch (DbUpdateException ex) when (TryTranslateEmployeeWriteException(ex, out var message))
            {
                _logger.LogWarning(ex, "Employee reactivation blocked by constraint");
                throw new InvalidOperationException(message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reactivating employee: {id}");
                throw;
            }
        }

        public async Task<List<EmployeeModel>> GetAllWorkersAsync(string filter = "Active")
        {
            try
            {
                var query = _context.Employees
                    .IgnoreQueryFilters()
                    .Include(e => e.Contractor)
                    .AsQueryable();
        
                switch (filter)
                {
                    case "Active":
                        query = query.Where(e => !e.IsDeleted);
                        break;
                    case "Deleted":
                        query = query.Where(e => e.IsDeleted);
                        break;
                    case "All":
                    default:
                        break;
                }
        
                return await query
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workers with filter: {filter}", filter);
                return new List<EmployeeModel>();
            }
        }

        public async Task<List<EmployeeModel>> GetActiveWorkersAsync()
        {
            try
            {
                return await _context.Employees
                    .Include(e => e.Contractor)
                    .Where(e => !e.IsDeleted)
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active workers");
                return new List<EmployeeModel>();
            }
        }

        public async Task<List<EmployeeModel>> GetDeletedWorkersAsync()
        {
            try
            {
                return await _context.Employees
                    .IgnoreQueryFilters()
                    .Include(e => e.Contractor)
                    .Where(e => e.IsDeleted)
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting deleted workers");
                return new List<EmployeeModel>();
            }
        }

        public async Task<List<EmployeeModel>> GetAvailableEmployeesAsync()
        {
            try
            {
                return await _context.Employees
                    .Where(e => !e.IsDeleted && e.IsAvailable)
                    .OrderBy(e => e.FirstName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available employees");
                return new List<EmployeeModel>();
            }
        }

        public async Task<List<EmployeeModel>> GetByContractorAsync(int contractorId)
        {
            try
            {
                return await _context.Employees
                    .Where(e => !e.IsDeleted && e.ContractorId == contractorId)
                    .OrderBy(e => e.FirstName)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting employees by contractor: {contractorId}");
                return new List<EmployeeModel>();
            }
        }

        public async Task<List<JobAssignmentModel>> GetActiveAssignmentsAsync(int employeeId)
        {
            try
            {
                return await _context.JobAssignments
                    .Where(a => a.EmployeeId == employeeId && a.Status != "Completed")
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active assignments for employee {employeeId}");
                return new List<JobAssignmentModel>();
            }
        }

        public async Task<List<JobAssignmentModel>> GetActiveAssignmentsWithDetailsAsync(int employeeId)
        {
            try
            {
                return await _context.JobAssignments
                    .Include(a => a.Job)
                    .Where(a => a.EmployeeId == employeeId && a.Status != "Completed")
                    .OrderBy(a => a.Job.ScheduledDate)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active assignments with details for employee {employeeId}");
                return new List<JobAssignmentModel>();
            }
        }

        private async Task<string> SaveImage(IFormFile image, string type, int contractorId)
        {
            if (image == null || image.Length == 0)
                return null;

            try
            {
                // Validate file size (max 5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    _logger.LogWarning($"Image too large: {image.Length} bytes");
                    throw new InvalidOperationException("Image file size must be less than 5MB.");
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    _logger.LogWarning($"Invalid image type: {image.ContentType}");
                    throw new InvalidOperationException("Only JPEG, PNG, GIF, and WEBP images are allowed.");
                }

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "employees", contractorId.ToString(), type);
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream).ConfigureAwait(false);
                }

                _logger.LogInformation($"Image saved: {filePath}");
                return $"/uploads/employees/{contractorId}/{type}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving image: {image?.FileName}");
                throw;
            }
        }

        private void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;

            try
            {
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Image deleted: {fullPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image: {imagePath}");
            }
        }

        private async Task EnsureEmployeeContactUniquenessAsync(
            int? employeeId,
            string firstName,
            string lastName,
            string phone1,
            string? phone2,
            string? email,
            string? panNumber)
        {
            if (!string.IsNullOrWhiteSpace(phone2) && phone1 == phone2)
            {
                throw new ArgumentException("Primary and secondary phone numbers must be different.");
            }

            var otherEmployees = await _context.Employees
                .IgnoreQueryFilters()
                .Where(e => !e.IsDeleted && (!employeeId.HasValue || e.Id != employeeId.Value))
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.Phone1,
                    e.Phone2,
                    e.Email,
                    e.PanNumber
                })
                .ToListAsync()
                .ConfigureAwait(false);

            if (otherEmployees.Any(e =>
                    string.Equals(e.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.LastName, lastName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("An active employee with this name already exists.");
            }

            if (otherEmployees.Any(e => e.Phone1 == phone1 || e.Phone2 == phone1))
            {
                throw new InvalidOperationException("Another active employee already uses this phone number.");
            }

            if (!string.IsNullOrWhiteSpace(phone2) &&
                otherEmployees.Any(e => e.Phone1 == phone2 || e.Phone2 == phone2))
            {
                throw new InvalidOperationException("Another active employee already uses the secondary phone number.");
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                otherEmployees.Any(e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("An active employee with this email already exists.");
            }

            if (!string.IsNullOrWhiteSpace(panNumber) &&
                otherEmployees.Any(e => string.Equals(e.PanNumber, panNumber, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("An active employee with this PAN number already exists.");
            }
        }

        private static string NormalizePhone(string phone) => phone.Trim();

        private static string? NormalizeOptionalPhone(string? phone) =>
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        private static string? NormalizeOptionalEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        private static string? NormalizeOptionalText(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool TryTranslateEmployeeWriteException(DbUpdateException ex, out string message)
        {
            if (ex.InnerException is PostgresException postgresException)
            {
                message = postgresException.ConstraintName switch
                {
                    "IX_Employees_Email_Unique" => "An active employee with this email already exists.",
                    "IX_Employees_FirstName_LastName_Unique" => "An active employee with this name already exists.",
                    "IX_Employees_Phone1_Unique" => "Another active employee already uses this phone number.",
                    "IX_Employees_Phone2_Unique" => "Another active employee already uses the secondary phone number.",
                    "IX_Employees_PanNumber_Unique" => "An active employee with this PAN number already exists.",
                    "CK_Employees_DistinctPhones" => "Primary and secondary phone numbers must be different.",
                    _ => string.Empty
                };

                return !string.IsNullOrWhiteSpace(message);
            }

            message = string.Empty;
            return false;
        }
    }
}

