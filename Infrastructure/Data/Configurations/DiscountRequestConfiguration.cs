using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for DiscountRequest
/// </summary>
public class DiscountRequestConfiguration : IEntityTypeConfiguration<DiscountRequest>
{
    public void Configure(EntityTypeBuilder<DiscountRequest> builder)
    {
        // Table name
        builder.ToTable("DiscountRequests");

        // Primary key
        builder.HasKey(dr => dr.Id);

        // Properties
        builder.Property(dr => dr.CustomerId)
            .IsRequired();

        builder.Property(dr => dr.SalespersonId)
            .IsRequired();

        builder.Property(dr => dr.RequestedDiscountPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(dr => dr.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(dr => dr.RiskScore)
            .HasPrecision(5, 2);

        builder.Property(dr => dr.EstimatedMarginPercentage)
            .HasPrecision(5, 2);

        builder.Property(dr => dr.CompanyId)
            .IsRequired();

        builder.Property(dr => dr.Comments)
            .HasMaxLength(2000);

        builder.Property(dr => dr.CreatedAt)
            .IsRequired();

        builder.Property(dr => dr.UpdatedAt);

        builder.Property(dr => dr.DecisionAt);

        // Owned collection - DiscountRequestItems
        builder.OwnsMany(dr => dr.Items, item =>
        {
            item.ToTable("DiscountRequestItems");

            item.WithOwner()
                .HasForeignKey("DiscountRequestId");

            item.Property<Guid>("Id")
                .ValueGeneratedOnAdd();

            item.HasKey("Id");

            item.Property(i => i.ProductId)
                .IsRequired();

            item.Property(i => i.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            item.Property(i => i.Quantity)
                .IsRequired();

            item.Property(i => i.DiscountPercentage)
                .HasPrecision(5, 2)
                .IsRequired();

            // Money value objects for UnitBasePrice
            item.OwnsOne(i => i.UnitBasePrice, money =>
            {
                money.Property(m => m.Value)
                    .HasColumnName("UnitBasePrice")
                    .HasPrecision(18, 4)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("UnitBasePriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // Money value objects for UnitFinalPrice
            item.OwnsOne(i => i.UnitFinalPrice, money =>
            {
                money.Property(m => m.Value)
                    .HasColumnName("UnitFinalPrice")
                    .HasPrecision(18, 4)
                    .IsRequired();

                money.Property(m => m.Currency)
                    .HasColumnName("UnitFinalPriceCurrency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            item.HasIndex("DiscountRequestId");
            item.HasIndex(i => i.ProductId);
        });

        // Relationships
        builder.HasOne(dr => dr.Customer)
            .WithMany()
            .HasForeignKey(dr => dr.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dr => dr.Salesperson)
            .WithMany()
            .HasForeignKey(dr => dr.SalespersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dr => dr.Company)
            .WithMany()
            .HasForeignKey(dr => dr.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(dr => new { dr.CompanyId, dr.Id });
        builder.HasIndex(dr => new { dr.CompanyId, dr.CustomerId });
        builder.HasIndex(dr => new { dr.CompanyId, dr.SalespersonId });
        builder.HasIndex(dr => dr.CreatedAt);
        builder.HasIndex(dr => dr.Status);
        builder.HasIndex(dr => dr.DecisionAt);
    }
}
