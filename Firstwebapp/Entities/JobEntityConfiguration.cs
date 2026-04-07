using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thekdar.Entities;

public class JobEntityConfiguration : IEntityTypeConfiguration<JobModel>
{
    public void Configure(EntityTypeBuilder<JobModel> entity)
    {
        // Table name (optional)
        entity.ToTable("Jobs");
        
        // Primary Key
        entity.HasKey(e => e.Id);
        
        // Indexes for performance
        entity.HasIndex(e => e.Status).HasDatabaseName("IX_Jobs_Status");
        entity.HasIndex(e => e.ScheduledDate).HasDatabaseName("IX_Jobs_ScheduledDate");
        entity.HasIndex(e => e.CreatedByUserId).HasDatabaseName("IX_Jobs_CreatedByUserId");
        entity.HasIndex(e => e.CompletedByUserId).HasDatabaseName("IX_Jobs_CompletedByUserId");
        
        // Property Configurations
        entity.Property(e => e.Title)
            .HasMaxLength(150)
            .IsRequired()
            .HasComment("Job title/summary");
            
        entity.Property(e => e.Description)
            .HasMaxLength(2000)
            .HasComment("Detailed job description");
            
        entity.Property(e => e.ClientName)
            .HasMaxLength(100)
            .HasComment("Client/customer name");
            
        entity.Property(e => e.Address)
            .HasMaxLength(200)
            .HasComment("Job site address");
            
        entity.Property(e => e.ScheduledDate)
            .HasComment("Scheduled date for the job (UTC)");
            
        entity.Property(e => e.Status)
            .HasConversion<string>()
            .HasDefaultValue(JobStatus.Active)
            .HasComment("Job status: Active, PendingConfirmation, or Completed");
            
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Job creation timestamp (UTC)");
            
        // Foreign Key Relationships - Fixed to use WithMany with navigation properties
        entity.HasOne(j => j.CreatedBy)
            .WithMany(u => u.JobsCreated)  // Links to UserModel.JobsCreated
            .HasForeignKey(j => j.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)  // Don't delete jobs if user is deleted
            .HasConstraintName("FK_Jobs_CreatedByUserId");
            
        entity.HasOne(j => j.CompletedBy)
            .WithMany(u => u.JobsCompleted)  // Links to UserModel.JobsCompleted
            .HasForeignKey(j => j.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict)  // Don't delete jobs if user is deleted
            .HasConstraintName("FK_Jobs_CompletedByUserId");
            
        // Ensure CreatedByUserId and CompletedByUserId can be null
        entity.Property(e => e.CreatedByUserId).IsRequired(false);
        entity.Property(e => e.CompletedByUserId).IsRequired(false);
    }
}