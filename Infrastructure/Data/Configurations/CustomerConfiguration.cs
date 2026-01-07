using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Customer
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Table name
        builder.ToTable("Customers");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Segment)
            .HasMaxLength(100);

        builder.Property(c => c.Classification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.CompanyId)
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.AdditionalInfo)
            .HasColumnType("nvarchar(max)");

        builder.Property(c => c.ExternalSystemId)
            .HasMaxLength(100);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Relationships
        builder.HasOne(c => c.Company)
            .WithMany()
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => new { c.CompanyId, c.Id });
        builder.HasIndex(c => new { c.CompanyId, c.ExternalSystemId });
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.Status);
    }
}
