using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLog
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table name
        builder.ToTable("AuditLogs");

        // Primary key
        builder.HasKey(al => al.Id);

        // Properties
        builder.Property(al => al.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.EntityId)
            .IsRequired();

        builder.Property(al => al.Action)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(al => al.Origin)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(al => al.UserId);

        builder.Property(al => al.Payload)
            .HasColumnType("nvarchar(max)");

        builder.Property(al => al.DateTime)
            .IsRequired();

        builder.Property(al => al.CompanyId)
            .IsRequired();

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.Metadata)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(al => al.Company)
            .WithMany()
            .HasForeignKey(al => al.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for multi-tenant isolation and audit queries
        builder.HasIndex(al => new { al.CompanyId, al.Id });
        builder.HasIndex(al => new { al.CompanyId, al.EntityName, al.EntityId });
        builder.HasIndex(al => new { al.CompanyId, al.UserId });
        builder.HasIndex(al => al.DateTime);
        builder.HasIndex(al => al.Action);
        builder.HasIndex(al => al.Origin);
    }
}
