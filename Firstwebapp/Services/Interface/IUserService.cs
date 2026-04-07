using Thekdar.Models;
using Thekdar.ViewModels;  // Add this!

namespace Thekdar.Services.Interface
{
    public interface IUserService
    {
        // Get all users
        Task<List<UserModel>> GetAllUsers();
        
        // Get one user
        Task<UserModel> GetUserById(int id);
        
        // Add a new user from UserModel (admin)
        Task AddUser(UserModel user);
        
        // NEW: Add user from RegisterViewModel (registration)
        Task AddUserFromViewModel(RegisterViewModel model);
        
        // Update a user
        Task UpdateUser(UserEditViewModel model);
        
        // Get only active users
        Task<List<UserModel>> GetActiveUsers();

        // Get only inactive users
        Task<List<UserModel>> GetInactiveUsers();
        
        // Activate a user
        Task ActivateUser(int id);
        
        // Deactivate a user
        Task DeactivateUser(int id);
    }
}
