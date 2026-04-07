using Thekdar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Thekdar.Entities
{
    public class PayrollEntityConfiguration : IEntityTypeConfiguration<PayrollModel>
    {
        public void Configure(EntityTypeBuilder<PayrollModel> builder)
        {
            builder.ToTable("Payrolls");
            
            builder.HasKey(p => p.Id);
            
            // Configure relationships
            builder.HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            builder.HasOne(p => p.Contractor)
                .WithMany()
                .HasForeignKey(p => p.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Configure decimal precision
            builder.Property(p => p.DailyRate)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.BaseWage)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.OvertimeWage)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.Bonus)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.Deduction)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.TotalWage)
                .HasColumnType("decimal(18,2)");
                
            builder.Property(p => p.NetPayable)
                .HasColumnType("decimal(18,2)");
            
            // Configure dates
            builder.Property(p => p.PeriodStart)
                .HasColumnType("date");
                
            builder.Property(p => p.PeriodEnd)
                .HasColumnType("date");
                
            builder.Property(p => p.PaymentDate)
                .HasColumnType("date");
                
            builder.Property(p => p.CreatedAt)
                .HasColumnType("timestamp with time zone");
            
            // Configure required fields
            builder.Property(p => p.EmployeeId)
                .IsRequired();
                
            builder.Property(p => p.ContractorId)
                .IsRequired();
                
            builder.Property(p => p.PeriodStart)
                .IsRequired();
                
            builder.Property(p => p.PeriodEnd)
                .IsRequired();
            
            // Configure indexes
            builder.HasIndex(p => p.EmployeeId);
            builder.HasIndex(p => p.ContractorId);
            builder.HasIndex(p => p.PeriodStart);
            builder.HasIndex(p => p.PeriodEnd);
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.PayslipNumber)
                .IsUnique();
        }
    }
}