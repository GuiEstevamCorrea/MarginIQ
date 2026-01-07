using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for RefreshToken entity.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        // Primary Key
        builder.HasKey(rt => rt.Id);

        // Properties
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500); // JWT refresh tokens can be long

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.CompanyId)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.IsActive)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .IsRequired(false);

        builder.Property(rt => rt.RevocationReason)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(rt => rt.DeviceId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired(false);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        // Indexes for performance and multi-tenancy
        builder.HasIndex(rt => new { rt.CompanyId, rt.UserId })
            .HasDatabaseName("IX_RefreshTokens_CompanyId_UserId");

        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => new { rt.CompanyId, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_CompanyId_ExpiresAt");

        builder.HasIndex(rt => new { rt.CompanyId, rt.IsActive })
            .HasDatabaseName("IX_RefreshTokens_CompanyId_IsActive");

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rt => rt.Company)
            .WithMany()
            .HasForeignKey(rt => rt.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}