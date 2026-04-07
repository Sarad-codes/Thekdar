using Thekdar.Models;

namespace Thekdar.Services.Interface
{
    public interface IEmployeeService
    {
      
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
    }
}
