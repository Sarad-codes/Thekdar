using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thekdar.Entities;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> entity)
    {
        // Table name (optional)
        entity.ToTable("Users");
        
        // Primary Key
        entity.HasKey(e => e.Id);
        
        // Indexes
        entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Users_Name");
        entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_Users_Email");
        entity.HasIndex(e => e.Phone).IsUnique().HasDatabaseName("IX_Users_Phone");
        entity.HasIndex(e => e.Status).HasDatabaseName("IX_Users_Status");
        entity.HasIndex(e => e.Role).HasDatabaseName("IX_Users_Role");
        
        // Property Configurations
        entity.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("User's full name");
            
        entity.Property(e => e.Email)
            .HasMaxLength(100)
            .IsRequired()
            .HasComment("User's email address - must be unique");
            
        entity.Property(e => e.Phone)
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("Phone number - exactly 10 digits");
            
        entity.Property(e => e.Age)
            .IsRequired()
            .HasComment("User's age - must be between 18-100");
            
        entity.Property(e => e.PasswordHash)
            .IsRequired()
            .HasComment("BCrypt hashed password");
            
        entity.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(UserStatus.Active)
            .HasComment("Account status: Active or Inactive");
            
        entity.Property(e => e.Role)
            .HasConversion<int>()
            .HasDefaultValue(UserRole.Contractor)
            .HasSentinel(UserRole.Contractor)  // Add this line
            .HasComment("User role: Admin or Contractor");
            
        entity.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Account creation timestamp");
            
        entity.Property(e => e.LastLoginAt)
            .HasComment("Last login timestamp");
            
        entity.Property(e => e.PasswordResetToken)
            .HasMaxLength(200)
            .HasComment("Password reset token");
            
        entity.Property(e => e.ResetTokenExpires)
            .HasComment("Password reset token expiration");
            
        entity.Property(e => e.ProfilePicture)
            .HasColumnType("bytea")
            .HasComment("Profile picture as byte array");
            
        entity.Property(e => e.ProfilePictureContentType)
            .HasMaxLength(50)
            .HasComment("MIME type of profile picture (image/jpeg, image/png, etc.)");
    }
}
