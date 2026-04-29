using Thekdar.Models;
using Thekdar.Entities;
using Microsoft.EntityFrameworkCore;

namespace Thekdar.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    
    public ApplicationDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from separate files
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new JobEntityConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new JobAssignmentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new PayrollEntityConfiguration());  
    }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<JobModel> Jobs { get; set; }
    public DbSet<EmployeeModel> Employees { get; set; }
    public DbSet<JobAssignmentModel> JobAssignments { get; set; }
    public DbSet<PayrollModel> Payrolls { get; set; }
    public DbSet<MobileAttendance> MobileAttendances { get; set; }
}