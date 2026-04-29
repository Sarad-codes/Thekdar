using Thekdar.Models;

namespace Thekdar.Services.Interface
{
    public interface IEmployeeService
    {
        // Existing methods...
        Task<List<EmployeeModel>> GetAllAsync();
        Task<EmployeeModel?> GetByIdAsync(int id);
        Task<EmployeeModel> CreateAsync(EmployeeViewModel model, int contractorId);
        Task UpdateAsync(EmployeeViewModel model);
        Task SoftDeleteAsync(int id);
        Task<List<EmployeeModel>> GetAvailableEmployeesAsync();
        Task<List<EmployeeModel>> GetByContractorAsync(int contractorId);
        Task<List<JobAssignmentModel>> GetActiveAssignmentsAsync(int employeeId);
        Task<List<JobAssignmentModel>> GetActiveAssignmentsWithDetailsAsync(int employeeId);
        Task<List<EmployeeModel>> GetAllWorkersAsync(string filter = "Active");
        Task<List<EmployeeModel>> GetActiveWorkersAsync();
        Task<List<EmployeeModel>> GetDeletedWorkersAsync();
        Task ReactivateAsync(int id);
        
        // ========== NEW MOBILE ACCESS METHODS ==========
        
        /// <summary>
        /// Enable mobile access for a worker and set/update password
        /// </summary>
        Task<bool> EnableMobileAccessAsync(int employeeId, string password);
        
        /// <summary>
        /// Disable mobile access for a worker
        /// </summary>
        Task<bool> DisableMobileAccessAsync(int employeeId);
        
        /// <summary>
        /// Update last mobile login timestamp
        /// </summary>
        Task UpdateLastMobileLoginAsync(int employeeId);
        
        /// <summary>
        /// Get worker by email for mobile login
        /// </summary>
        Task<EmployeeModel?> GetByEmailForMobileAsync(string email);
        
        /// <summary>
        /// Verify worker mobile password
        /// </summary>
        Task<bool> VerifyMobilePasswordAsync(string email, string password);
    }
}