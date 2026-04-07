using Thekdar.Data;
using Thekdar.Models;
using Thekdar.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Thekdar.Services;

public class JobService : IJobService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<JobService> _logger;

    public JobService(ApplicationDbContext context, ILogger<JobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<JobModel>> GetAllAsync()
    {
        try
        {
            return await _context.Jobs
                .OrderByDescending(j => j.ScheduledDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all jobs");
            return new List<JobModel>();
        }
    }

    public async Task<List<JobModel>> GetAllWithUsersAsync()
    {
        try
        {
            return await _context.Jobs
                .Include(j => j.CreatedBy)
                .Include(j => j.CompletedBy)
                .OrderByDescending(j => j.ScheduledDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs with users");
            return new List<JobModel>();
        }
    }

    public async Task<JobModel?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Jobs.FindAsync(id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job by ID: {JobId}", id);
            return null;
        }
    }

    public async Task<JobModel?> GetByIdWithUsersAsync(int id)
    {
        try
        {
            return await _context.Jobs
                .Include(j => j.CreatedBy)
                .Include(j => j.CompletedBy)
                .FirstOrDefaultAsync(j => j.Id == id)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job with users by ID: {JobId}", id);
            return null;
        }
    }

    public async Task<JobModel> CreateAsync(JobModel job, int createdByUserId)
    {
        try
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));
            job.CreatedAt = DateTime.UtcNow;
            job.Status = JobStatus.Active;
            job.CreatedByUserId = createdByUserId;

            if (job.ScheduledDate.HasValue)
                job.ScheduledDate = DateTime.SpecifyKind(job.ScheduledDate.Value, DateTimeKind.Utc);

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Job created: {JobId} - {Title} by user {UserId}", job.Id, job.Title, createdByUserId);
            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job: {Title}", job?.Title);
            throw;
        }
    }

    public async Task UpdateAsync(JobModel job)
    {
        try
        {
            var existing = await _context.Jobs.FindAsync(job.Id).ConfigureAwait(false);
            if (existing == null)
            {
                _logger.LogWarning("Job not found for update: {JobId}", job.Id);
                return;
            }

            if (existing.Status == JobStatus.Completed)
                throw new InvalidOperationException("Cannot edit a completed job.");

            if (job.ScheduledDate.HasValue)
                job.ScheduledDate = DateTime.SpecifyKind(job.ScheduledDate.Value, DateTimeKind.Utc);

            existing.Title = job.Title;
            existing.Description = job.Description;
            existing.ClientName = job.ClientName;
            existing.Address = job.Address;
            existing.ScheduledDate = job.ScheduledDate;

            await _context.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Job updated: {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job: {JobId}", job.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(id).ConfigureAwait(false);
            if (job == null)
            {
                _logger.LogWarning("Job not found for deletion: {JobId}", id);
                return;
            }

            var assignments = await _context.JobAssignments
                .Where(a => a.JobId == id)
                .ToListAsync()
                .ConfigureAwait(false);

            if (assignments.Any())
                _context.JobAssignments.RemoveRange(assignments);

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Job deleted: {JobId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job: {JobId}", id);
            throw;
        }
    }

    public async Task<List<JobModel>> GetTodaysJobsAsync()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.Jobs
                .Where(j => j.ScheduledDate.HasValue && j.ScheduledDate.Value >= today && j.ScheduledDate.Value < tomorrow)
                .OrderBy(j => j.ScheduledDate)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's jobs");
            return new List<JobModel>();
        }
    }

    public async Task MarkPendingAsync(int id, int userId)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(id).ConfigureAwait(false);
            if (job == null)
            {
                _logger.LogWarning("Job not found for marking pending: {JobId}", id);
                return;
            }

            if (job.Status != JobStatus.Active)
                throw new InvalidOperationException($"Cannot mark a job with status '{job.Status}' as pending.");

            job.Status = JobStatus.PendingConfirmation;
            job.CompletedByUserId = userId;
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job pending: {JobId}", id);
            throw;
        }
    }

    public async Task ConfirmCompleteAsync(int id, int userId)
    {
        try
        {
            var job = await _context.Jobs.FindAsync(id).ConfigureAwait(false);
            if (job == null)
            {
                _logger.LogWarning("Job not found for confirmation: {JobId}", id);
                return;
            }

            if (job.Status != JobStatus.PendingConfirmation)
                throw new InvalidOperationException($"Cannot confirm a job with status '{job.Status}' as complete.");

            job.Status = JobStatus.Completed;
            job.CompletedByUserId = userId;
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming job complete: {JobId}", id);
            throw;
        }
    }

    public async Task<List<int>> GetAssignedEmployeeIdsAsync(int jobId)
    {
        try
        {
            return await _context.JobAssignments
                .Where(a => a.JobId == jobId)
                .Select(a => a.EmployeeId)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned employee IDs for job: {JobId}", jobId);
            return new List<int>();
        }
    }

    public async Task<int> GetAssignedEmployeeCountAsync(int jobId)
    {
        try
        {
            return await _context.JobAssignments
                .Where(a => a.JobId == jobId)
                .CountAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned employee count for job: {JobId}", jobId);
            return 0;
        }
    }

    public async Task<JobAssignmentOperationResult> AssignEmployeesAsync(int jobId, List<int> employeeIds, Dictionary<int, string> roles, int assignedByUserId, bool isAdmin)
    {
        var result = new JobAssignmentOperationResult();

        try
        {
            var job = await _context.Jobs
                .Include(j => j.CreatedBy)
                .FirstOrDefaultAsync(j => j.Id == jobId)
                .ConfigureAwait(false);

            if (job == null)
                throw new InvalidOperationException("Job not found.");

            if (job.Status == JobStatus.Completed)
                throw new InvalidOperationException("Cannot assign employees to a completed job.");

            if (!job.CreatedByUserId.HasValue)
                throw new InvalidOperationException("Job has no associated contractor.");

            var ownerId = job.CreatedByUserId.Value;
            var assignedAt = DateTime.UtcNow;

            var existingAssignments = await _context.JobAssignments
                .Where(a => a.JobId == jobId)
                .ToListAsync()
                .ConfigureAwait(false);

            _context.JobAssignments.RemoveRange(existingAssignments);

            if (employeeIds != null && employeeIds.Any())
            {
                foreach (var empId in employeeIds)
                {
                    var employee = await _context.Employees
                        .FirstOrDefaultAsync(e => e.Id == empId && !e.IsDeleted)
                        .ConfigureAwait(false);

                    if (employee == null)
                        throw new InvalidOperationException("One or more selected workers could not be found.");

                    if (employee.ContractorId != ownerId)
                    {
                        if (isAdmin)
                        {
                            _logger.LogInformation(
                                "Admin {AssignedByUserId} assigned employee {EmployeeId} from contractor {EmployeeContractorId} to job {JobId} owned by {OwnerId}",
                                assignedByUserId, empId, employee.ContractorId, jobId, ownerId);
                        }
                        else
                        {
                            var previousContractorId = employee.ContractorId;
                            employee.ContractorId = ownerId;
                            result.Warnings.Add($"{employee.FullName} was automatically linked to {job.CreatedBy?.Name ?? "this contractor"} before assignment.");
                            _logger.LogWarning(
                                "Contractor {AssignedByUserId} moved employee {EmployeeId} from contractor {OldContractorId} to {NewContractorId} for job {JobId}",
                                assignedByUserId, empId, previousContractorId, ownerId, jobId);
                        }
                    }

                    var role = roles != null && roles.TryGetValue(empId, out var selectedRole) ? selectedRole : "Assistant";

                    _context.JobAssignments.Add(new JobAssignmentModel
                    {
                        JobId = jobId,
                        EmployeeId = empId,
                        AssignedByUserId = assignedByUserId,
                        AssignedDate = assignedAt,
                        AssignedAt = assignedAt,
                        Role = role,
                        Status = "Assigned"
                    });

                    result.AssignedEmployeeIds.Add(empId);
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Assigned {Count} employees to job: {JobId}", result.AssignedEmployeeIds.Count, jobId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning employees to job: {JobId}", jobId);
            throw;
        }
    }

    public async Task<List<EmployeeModel>> GetAssignedEmployeesWithDetailsAsync(int jobId)
    {
        try
        {
            var assignments = await _context.JobAssignments
                .Where(a => a.JobId == jobId)
                .Select(a => a.EmployeeId)
                .ToListAsync()
                .ConfigureAwait(false);

            if (!assignments.Any())
                return new List<EmployeeModel>();

            return await _context.Employees
                .Where(e => assignments.Contains(e.Id) && !e.IsDeleted)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned employees for job: {JobId}", jobId);
            return new List<EmployeeModel>();
        }
    }

    public async Task<List<JobAssignmentModel>> GetAssignmentsForJobAsync(int jobId)
    {
        try
        {
            return await _context.JobAssignments
                .Include(a => a.Employee)
                .Include(a => a.AssignedByUser)
                .Where(a => a.JobId == jobId)
                .OrderBy(a => a.AssignedAt)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for job: {JobId}", jobId);
            return new List<JobAssignmentModel>();
        }
    }

    public async Task<JobAssignmentModel?> GetAssignmentRoleAsync(int jobId, int employeeId)
    {
        try
        {
            return await _context.JobAssignments
                .Include(a => a.AssignedByUser)
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.EmployeeId == employeeId)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment role for job {JobId}, employee {EmployeeId}", jobId, employeeId);
            return null;
        }
    }

    public async Task UnassignEmployeeAsync(int jobId, int employeeId)
    {
        try
        {
            var assignment = await _context.JobAssignments
                .FirstOrDefaultAsync(a => a.JobId == jobId && a.EmployeeId == employeeId)
                .ConfigureAwait(false);

            if (assignment == null)
                throw new InvalidOperationException("Employee is not assigned to this job.");

            _context.JobAssignments.Remove(assignment);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning employee {EmployeeId} from job {JobId}", employeeId, jobId);
            throw;
        }
    }

    public async Task UnassignAllEmployeesAsync(int jobId)
    {
        try
        {
            var assignments = await _context.JobAssignments
                .Where(a => a.JobId == jobId)
                .ToListAsync()
                .ConfigureAwait(false);

            if (!assignments.Any())
                return;

            _context.JobAssignments.RemoveRange(assignments);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning all employees from job {JobId}", jobId);
            throw;
        }
    }
}


