using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Product
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Table name
        builder.ToTable("Products");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Category)
            .HasMaxLength(100);

        // Money value object - owned entity
        builder.OwnsOne(p => p.BasePrice, money =>
        {
            money.Property(m => m.Value)
                .HasColumnName("BasePrice")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("BasePriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.BaseMarginPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(p => p.CompanyId)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Sku)
            .HasMaxLength(100);

        builder.Property(p => p.AdditionalInfo)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Relationships
        builder.HasOne(p => p.Company)
            .WithMany()
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => new { p.CompanyId, p.Id });
        builder.HasIndex(p => new { p.CompanyId, p.Sku });
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.Category);
    }
}
