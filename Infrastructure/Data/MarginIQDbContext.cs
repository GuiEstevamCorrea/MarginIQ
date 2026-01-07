using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Main database context for the MarginIQ application.
/// Manages all aggregate roots and provides access to the SQL Server database.
/// </summary>
public class MarginIQDbContext : DbContext
{
    /// <summary>
    /// Companies (Tenants) in the multi-tenant system
    /// </summary>
    public DbSet<Company> Companies { get; set; } = null!;

    /// <summary>
    /// Users belonging to companies with specific roles
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Customers for each company
    /// </summary>
    public DbSet<Customer> Customers { get; set; } = null!;

    /// <summary>
    /// Products available in the system
    /// </summary>
    public DbSet<Product> Products { get; set; } = null!;

    /// <summary>
    /// Discount requests created by salespeople
    /// </summary>
    public DbSet<DiscountRequest> DiscountRequests { get; set; } = null!;

    /// <summary>
    /// Approvals for discount requests
    /// </summary>
    public DbSet<Approval> Approvals { get; set; } = null!;

    /// <summary>
    /// Business rules configured per company
    /// </summary>
    public DbSet<BusinessRule> BusinessRules { get; set; } = null!;

    /// <summary>
    /// AI learning data captured from decisions
    /// </summary>
    public DbSet<AILearningData> AILearningData { get; set; } = null!;

    /// <summary>
    /// Audit logs for compliance and traceability
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Refresh tokens for JWT authentication
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Constructor that injects DbContextOptions
    /// </summary>
    /// <param name="options">Database context configuration options</param>
    public MarginIQDbContext(DbContextOptions<MarginIQDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configures entity mappings using Fluent API
    /// </summary>
    /// <param name="modelBuilder">The model builder instance</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarginIQDbContext).Assembly);
    }
}
