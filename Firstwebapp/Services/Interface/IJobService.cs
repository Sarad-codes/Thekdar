using Thekdar.Models;

namespace Thekdar.Services.Interface;

public interface IJobService
{
    Task<List<JobModel>> GetAllAsync();
    Task<List<JobModel>> GetAllWithUsersAsync();
    Task<JobModel?> GetByIdAsync(int id);
    Task<JobModel?> GetByIdWithUsersAsync(int id);
    Task<JobModel> CreateAsync(JobModel job, int createdByUserId);
    Task UpdateAsync(JobModel job);
    Task DeleteAsync(int id);
    Task<List<JobModel>> GetTodaysJobsAsync();
    Task MarkPendingAsync(int id, int userId);
    Task ConfirmCompleteAsync(int id, int userId);
    Task<int> GetAssignedEmployeeCountAsync(int jobId);
    Task<List<EmployeeModel>> GetAssignedEmployeesWithDetailsAsync(int jobId);
    Task<List<JobAssignmentModel>> GetAssignmentsForJobAsync(int jobId);
    Task<JobAssignmentModel?> GetAssignmentRoleAsync(int jobId, int employeeId);
    Task<List<int>> GetAssignedEmployeeIdsAsync(int jobId);
    Task<JobAssignmentOperationResult> AssignEmployeesAsync(int jobId, List<int> employeeIds, Dictionary<int, string> roles, int assignedByUserId, bool isAdmin);
    Task UnassignEmployeeAsync(int jobId, int employeeId);
    Task UnassignAllEmployeesAsync(int jobId);
}

