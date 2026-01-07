using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Security;

namespace Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for seeding test data in MarginIQDbContext.
/// This is for development/testing purposes only.
/// </summary>
public static class DbContextSeedExtensions
{
    /// <summary>
    /// Seeds the database with initial test data including a test user for authentication.
    /// Should only be used in development environment.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="passwordHasher">Password hasher service</param>
    public static async Task SeedTestDataAsync(this MarginIQDbContext context, BCryptPasswordHasher passwordHasher)
    {
        // Check if data already exists
        if (context.Companies.Any())
            return;

        // Create test company
        var testCompany = new Company(
            name: "Test Company Ltd.",
            segment: CompanySegment.Manufacturing
        );

        context.Companies.Add(testCompany);
        await context.SaveChangesAsync();

        // Create test user for login testing
        var testUser = new User(
            name: "Test Admin",
            email: "admin@test.com",
            role: UserRole.Admin,
            companyId: testCompany.Id,
            passwordHash: passwordHasher.Hash("admin123") // Default test password
        );

        context.Users.Add(testUser);

        // Create test salesperson
        var testSalesperson = new User(
            name: "Test Salesperson",
            email: "sales@test.com",
            role: UserRole.Salesperson,
            companyId: testCompany.Id,
            passwordHash: passwordHasher.Hash("sales123")
        );

        context.Users.Add(testSalesperson);

        // Create test manager
        var testManager = new User(
            name: "Test Manager",
            email: "manager@test.com",
            role: UserRole.Manager,
            companyId: testCompany.Id,
            passwordHash: passwordHasher.Hash("manager123")
        );

        context.Users.Add(testManager);

        await context.SaveChangesAsync();
    }
}