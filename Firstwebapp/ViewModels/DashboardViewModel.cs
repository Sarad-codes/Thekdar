using Thekdar.Models;

namespace Thekdar.ViewModels
{
    public class DashboardViewModel
    {
        // Summary Cards
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        
        // Stats (placeholder for now)
        public int ActiveJobs { get; set; }
        public int PendingPayments { get; set; }
        public decimal OutstandingAmount { get; set; }
        
        // Lists
        public List<UserProfileViewModel> RecentUsers { get; set; }
        public List<dynamic> TodaysJobs { get; set; } // Will replace with JobViewModel later
        
        // Weekly summary
        public int JobsDoneThisWeek { get; set; }
        public decimal CollectedThisWeek { get; set; }
        
        // Greeting
        public string Greeting { get; set; }
        public string CurrentDate { get; set; }
    }
}