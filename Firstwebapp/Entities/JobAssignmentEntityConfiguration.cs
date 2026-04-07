using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thekdar.Entities;

public class JobAssignmentEntityConfiguration : IEntityTypeConfiguration<JobAssignmentModel>
{
    public void Configure(EntityTypeBuilder<JobAssignmentModel> entity)
    {
        entity.ToTable("JobAssignments");

        entity.HasKey(ja => ja.Id);

        entity.Property(ja => ja.Role)
            .HasMaxLength(20)
            .HasDefaultValue("Assistant")
            .HasComment("Role: Lead, Assistant, Apprentice");

        entity.Property(ja => ja.Status)
            .HasMaxLength(20)
            .HasDefaultValue("Assigned")
            .HasComment("Status: Assigned, Working, Completed");

        entity.Property(ja => ja.HoursWorked)
            .HasPrecision(18, 2)
            .HasDefaultValue(0)
            .HasComment("Total hours worked on this job");

        entity.Property(ja => ja.AssignedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("When the employee was assigned");

        entity.Property(ja => ja.AssignedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("When the assignment record was created");

        entity.Property(ja => ja.StartDate)
            .HasComment("When work started (optional)");

        entity.Property(ja => ja.EndDate)
            .HasComment("When work ended (optional)");

        entity.HasIndex(ja => ja.JobId).HasDatabaseName("IX_JobAssignments_JobId");
        entity.HasIndex(ja => ja.EmployeeId).HasDatabaseName("IX_JobAssignments_EmployeeId");
        entity.HasIndex(ja => ja.AssignedByUserId).HasDatabaseName("IX_JobAssignments_AssignedByUserId");
        entity.HasIndex(ja => ja.Status).HasDatabaseName("IX_JobAssignments_Status");

        entity.HasIndex(ja => new { ja.JobId, ja.EmployeeId })
            .IsUnique()
            .HasDatabaseName("IX_JobAssignments_JobId_EmployeeId_Unique");

        entity.HasOne(ja => ja.Job)
            .WithMany()
            .HasForeignKey(ja => ja.JobId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_JobAssignments_JobId");

        entity.HasOne(ja => ja.Employee)
            .WithMany(e => e.JobAssignments)
            .HasForeignKey(ja => ja.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false)
            .HasConstraintName("FK_JobAssignments_EmployeeId");

        entity.HasOne(ja => ja.AssignedByUser)
            .WithMany()
            .HasForeignKey(ja => ja.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_JobAssignments_AssignedByUserId");
    }
}
