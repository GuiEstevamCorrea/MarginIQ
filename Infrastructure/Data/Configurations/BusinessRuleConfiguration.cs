using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for BusinessRule
/// </summary>
public class BusinessRuleConfiguration : IEntityTypeConfiguration<BusinessRule>
{
    public void Configure(EntityTypeBuilder<BusinessRule> builder)
    {
        // Table name
        builder.ToTable("BusinessRules");

        // Primary key
        builder.HasKey(br => br.Id);

        // Properties
        builder.Property(br => br.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(br => br.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(br => br.Scope)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(br => br.TargetEntityId);

        builder.Property(br => br.TargetIdentifier)
            .HasMaxLength(100);

        builder.Property(br => br.Parameters)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(br => br.CompanyId)
            .IsRequired();

        builder.Property(br => br.IsActive)
            .IsRequired();

        builder.Property(br => br.Priority)
            .IsRequired();

        builder.Property(br => br.CreatedAt)
            .IsRequired();

        builder.Property(br => br.UpdatedAt);

        builder.Property(br => br.CreatedByUserId);

        // Relationships
        builder.HasOne(br => br.Company)
            .WithMany()
            .HasForeignKey(br => br.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(br => br.CreatedByUser)
            .WithMany()
            .HasForeignKey(br => br.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(br => new { br.CompanyId, br.Id });
        builder.HasIndex(br => new { br.CompanyId, br.Type, br.IsActive });
        builder.HasIndex(br => new { br.CompanyId, br.Scope, br.IsActive });
        builder.HasIndex(br => br.CreatedAt);
        builder.HasIndex(br => br.Priority);
    }
}
