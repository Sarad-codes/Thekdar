namespace Thekdar.ViewModels;

public class EmployeeJobAssignmentViewModel
{
    
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string JobLocation { get; set; }
        public DateTime AssignedDate { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }
        public decimal HoursWorked { get; set; }
    
}