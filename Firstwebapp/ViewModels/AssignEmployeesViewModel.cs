using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class AssignEmployeesViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public List<EmployeeCheckboxViewModel> Employees { get; set; } = new();
    }

    public class EmployeeCheckboxViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Trade { get; set; }
        public decimal DailyRate { get; set; }
        public bool IsAssigned { get; set; }
        public string SelectedRole { get; set; } = "Assistant";
    }
}
