using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thekdar.Entities;

public class EmployeeEntityConfiguration : IEntityTypeConfiguration<EmployeeModel>
{
    public void Configure(EntityTypeBuilder<EmployeeModel> entity)
    {
        // Table name (optional, defaults to class name)
        entity.ToTable("Employees", table =>
        {
            table.HasCheckConstraint(
                "CK_Employees_DistinctPhones",
                "\"Phone2\" IS NULL OR \"Phone1\" <> \"Phone2\"");
        });
        
        // Primary Key
        entity.HasKey(e => e.Id);
        
        // Property Configurations
        entity.Property(e => e.FirstName)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Employee's first name");
            
        entity.Property(e => e.LastName)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Employee's last name");
            
        entity.Property(e => e.Trade)
            .HasMaxLength(50)
            .IsRequired()
            .HasComment("Employee's trade/specialization (Electrician, Plumber, etc.)");
            
        entity.Property(e => e.Phone1)
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("Primary phone number - exactly 10 digits");
        
        entity.Property(e => e.Phone2)
            .HasMaxLength(10)
            .HasComment("Secondary phone number - optional, exactly 10 digits if provided");
            
        entity.Property(e => e.Email)
            .HasMaxLength(100)
            .HasComment("Employee's email address");
            
        entity.Property(e => e.DailyRate)
            .HasPrecision(18, 2)
            .HasDefaultValue(0)
            .HasComment("Hourly rate in dollars");
            
        entity.Property(e => e.IsAvailable)
            .HasDefaultValue(true)
            .HasComment("Whether employee is available for work");
            
        // Soft delete flag
        entity.Property(e => e.IsDeleted)
            .HasDefaultValue(false)
            .HasComment("Soft delete flag - true means employee is deactivated");
            
        // PAN Details
        entity.Property(e => e.PanNumber)
            .HasMaxLength(20)
            .HasComment("PAN number - optional");
            
        entity.Property(e => e.PanCardImagePath)
            .HasMaxLength(500)
            .HasComment("Path to PAN card image file");
            
        entity.Property(e => e.ProfilePicturePath)
            .HasMaxLength(500)
            .HasComment("Path to employee profile picture");
            
        entity.Property(e => e.HireDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Date employee was hired");
            
        // Foreign Key - Contractor (User)
        entity.HasOne(e => e.Contractor)
            .WithMany(u => u.Employees)
            .HasForeignKey(e => e.ContractorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Employees_ContractorId");
            
        // Indexes for performance
        entity.HasIndex(e => e.ContractorId)
            .HasDatabaseName("IX_Employees_ContractorId");
            
        entity.HasIndex(e => e.IsAvailable)
            .HasDatabaseName("IX_Employees_IsAvailable");
            
        entity.HasIndex(e => e.Trade)
            .HasDatabaseName("IX_Employees_Trade");
            
        entity.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_Employees_IsDeleted");

        entity.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Email_Unique")
            .HasFilter("\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL");

        entity.HasIndex(e => new { e.FirstName, e.LastName })
            .IsUnique()
            .HasDatabaseName("IX_Employees_FirstName_LastName_Unique")
            .HasFilter("\"IsDeleted\" = FALSE");

        entity.HasIndex(e => e.Phone1)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Phone1_Unique")
            .HasFilter("\"IsDeleted\" = FALSE");

        entity.HasIndex(e => e.Phone2)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Phone2_Unique")
            .HasFilter("\"IsDeleted\" = FALSE AND \"Phone2\" IS NOT NULL");

        entity.HasIndex(e => e.PanNumber)
            .IsUnique()
            .HasDatabaseName("IX_Employees_PanNumber_Unique")
            .HasFilter("\"IsDeleted\" = FALSE AND \"PanNumber\" IS NOT NULL");
            
        // Composite index for common queries
        entity.HasIndex(e => new { e.ContractorId, e.IsAvailable, e.IsDeleted })
            .HasDatabaseName("IX_Employees_Contractor_Available_Deleted");
            
        // Query filter for soft delete (automatically excludes deleted employees)
        entity.HasQueryFilter(e => !e.IsDeleted);
    }
}
