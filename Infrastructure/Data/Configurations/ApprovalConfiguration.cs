using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Approval
/// </summary>
public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        // Table name
        builder.ToTable("Approvals");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.DiscountRequestId)
            .IsRequired();

        builder.Property(a => a.ApproverId);

        builder.Property(a => a.Decision)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Justification)
            .HasMaxLength(2000);

        builder.Property(a => a.SlaTimeInSeconds)
            .IsRequired();

        builder.Property(a => a.DecisionDateTime)
            .IsRequired();

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(a => a.DiscountRequest)
            .WithMany()
            .HasForeignKey(a => a.DiscountRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Approver)
            .WithMany()
            .HasForeignKey(a => a.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.DiscountRequestId);
        builder.HasIndex(a => a.ApproverId);
        builder.HasIndex(a => a.DecisionDateTime);
        builder.HasIndex(a => a.Decision);
        builder.HasIndex(a => a.Source);
    }
}
