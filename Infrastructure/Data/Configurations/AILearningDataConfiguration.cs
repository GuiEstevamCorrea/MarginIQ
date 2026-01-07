using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AILearningData
/// </summary>
public class AILearningDataConfiguration : IEntityTypeConfiguration<AILearningData>
{
    public void Configure(EntityTypeBuilder<AILearningData> builder)
    {
        // Table name
        builder.ToTable("AILearningData");

        // Primary key
        builder.HasKey(ald => ald.Id);

        // Properties
        builder.Property(ald => ald.CompanyId)
            .IsRequired();

        builder.Property(ald => ald.DiscountRequestId)
            .IsRequired();

        builder.Property(ald => ald.CustomerId)
            .IsRequired();

        builder.Property(ald => ald.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ald => ald.CustomerSegment)
            .HasMaxLength(100);

        builder.Property(ald => ald.CustomerClassification)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ald => ald.SalespersonId)
            .IsRequired();

        builder.Property(ald => ald.SalespersonName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(ald => ald.SalespersonRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ald => ald.ProductsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(ald => ald.RequestedDiscountPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(ald => ald.ApprovedDiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(ald => ald.BaseMarginPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(ald => ald.FinalMarginPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(ald => ald.TotalBasePrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(ald => ald.TotalFinalPrice)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(ald => ald.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(ald => ald.Decision)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ald => ald.DecisionSource)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ald => ald.RiskScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(ald => ald.AIConfidence)
            .HasPrecision(5, 4);

        builder.Property(ald => ald.SaleOutcome);

        builder.Property(ald => ald.SaleOutcomeDate);

        builder.Property(ald => ald.SaleOutcomeReason)
            .HasMaxLength(500);

        builder.Property(ald => ald.DecisionTimeSec)
            .IsRequired();

        builder.Property(ald => ald.ContextJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(ald => ald.RequestCreatedAt)
            .IsRequired();

        builder.Property(ald => ald.DecisionMadeAt)
            .IsRequired();

        builder.Property(ald => ald.RecordedAt)
            .IsRequired();

        builder.Property(ald => ald.UsedForTraining)
            .IsRequired();

        builder.Property(ald => ald.TrainedAt);

        // Relationships
        builder.HasOne(ald => ald.Company)
            .WithMany()
            .HasForeignKey(ald => ald.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ald => ald.DiscountRequest)
            .WithMany()
            .HasForeignKey(ald => ald.DiscountRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for multi-tenant isolation and AI queries
        builder.HasIndex(ald => new { ald.CompanyId, ald.Id });
        builder.HasIndex(ald => new { ald.CompanyId, ald.CustomerId });
        builder.HasIndex(ald => new { ald.CompanyId, ald.SalespersonId });
        builder.HasIndex(ald => new { ald.CompanyId, ald.Decision });
        builder.HasIndex(ald => new { ald.CompanyId, ald.UsedForTraining });
        builder.HasIndex(ald => ald.RequestCreatedAt);
        builder.HasIndex(ald => ald.DecisionMadeAt);
        builder.HasIndex(ald => ald.RecordedAt);
    }
}
