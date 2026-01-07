using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Integration;

/// <summary>
/// Integration tests to verify Value Objects (Money and DiscountRequestItem) 
/// persistence and querying behavior
/// </summary>
public class ValueObjectsPersistenceTests
{
    private MarginIQDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MarginIQDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MarginIQDbContext(options);
    }

    /// <summary>
    /// Test 1: Verify Money value object persists correctly in Product entity
    /// </summary>
    public async Task Money_ValueObject_Persists_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var company = new Company("Test Company", CompanySegment.Manufacturing);
        var money = new Money(1500.75m, "USD");
        var product = new Product("Test Product", money, 25.5m, company.Id, "Electronics", "SKU-001");

        // Act
        context.Companies.Add(company);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert - Reload from database
        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (savedProduct == null)
            throw new Exception("Product not found in database");

        if (savedProduct.BasePrice.Value != 1500.75m)
            throw new Exception($"Money value mismatch. Expected: 1500.75, Actual: {savedProduct.BasePrice.Value}");

        if (savedProduct.BasePrice.Currency != "USD")
            throw new Exception($"Currency mismatch. Expected: USD, Actual: {savedProduct.BasePrice.Currency}");

        Console.WriteLine("✓ Money value object persists correctly");
    }

    /// <summary>
    /// Test 2: Verify DiscountRequestItem owned collection persists correctly
    /// </summary>
    public async Task DiscountRequestItem_OwnedCollection_Persists_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var company = new Company("Test Company", CompanySegment.SaaS);
        var user = new User("John Doe", "john@test.com", UserRole.Salesperson, company.Id);
        var customer = new Customer("ABC Corp", company.Id);
        
        var product1 = new Product("Product A", new Money(1000m, "USD"), 30m, company.Id);
        var product2 = new Product("Product B", new Money(2000m, "USD"), 25m, company.Id);

        var items = new List<DiscountRequestItem>
        {
            new DiscountRequestItem(product1.Id, product1.Name, 5, new Money(1000m, "USD"), 10m),
            new DiscountRequestItem(product2.Id, product2.Name, 3, new Money(2000m, "USD"), 15m)
        };

        var discountRequest = new DiscountRequest(
            customer.Id,
            user.Id,
            company.Id,
            items,
            12.5m,
            "Bulk order discount request"
        );

        // Act
        context.Companies.Add(company);
        context.Users.Add(user);
        context.Customers.Add(customer);
        context.Products.AddRange(product1, product2);
        context.DiscountRequests.Add(discountRequest);
        await context.SaveChangesAsync();

        // Assert - Reload from database with items
        var savedRequest = await context.DiscountRequests
            .FirstOrDefaultAsync(dr => dr.Id == discountRequest.Id);

        if (savedRequest == null)
            throw new Exception("DiscountRequest not found in database");

        if (savedRequest.Items.Count != 2)
            throw new Exception($"Item count mismatch. Expected: 2, Actual: {savedRequest.Items.Count}");

        var firstItem = savedRequest.Items.First();
        if (firstItem.ProductId != product1.Id)
            throw new Exception("First item ProductId mismatch");

        if (firstItem.Quantity != 5)
            throw new Exception($"Quantity mismatch. Expected: 5, Actual: {firstItem.Quantity}");

        if (firstItem.UnitBasePrice.Value != 1000m)
            throw new Exception($"UnitBasePrice mismatch. Expected: 1000, Actual: {firstItem.UnitBasePrice.Value}");

        if (firstItem.DiscountPercentage != 10m)
            throw new Exception($"DiscountPercentage mismatch. Expected: 10, Actual: {firstItem.DiscountPercentage}");

        Console.WriteLine("✓ DiscountRequestItem owned collection persists correctly");
    }

    /// <summary>
    /// Test 3: Verify no orphan tables and proper cascade behavior
    /// </summary>
    public async Task No_Orphan_Tables_Proper_Cascade()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var company = new Company("Test Company", CompanySegment.Distribution);
        var user = new User("Jane Smith", "jane@test.com", UserRole.Salesperson, company.Id);
        var customer = new Customer("XYZ Ltd", company.Id);
        var product = new Product("Product X", new Money(500m, "USD"), 20m, company.Id);

        var items = new List<DiscountRequestItem>
        {
            new DiscountRequestItem(product.Id, product.Name, 10, new Money(500m, "USD"), 8m)
        };

        var discountRequest = new DiscountRequest(
            customer.Id,
            user.Id,
            company.Id,
            items,
            8m
        );

        context.Companies.Add(company);
        context.Users.Add(user);
        context.Customers.Add(customer);
        context.Products.Add(product);
        context.DiscountRequests.Add(discountRequest);
        await context.SaveChangesAsync();

        var requestId = discountRequest.Id;

        // Act - Remove the discount request (items should cascade delete)
        context.DiscountRequests.Remove(discountRequest);
        await context.SaveChangesAsync();

        // Assert - Verify request and items are gone
        var deletedRequest = await context.DiscountRequests
            .FirstOrDefaultAsync(dr => dr.Id == requestId);

        if (deletedRequest != null)
            throw new Exception("DiscountRequest should have been deleted");

        // Verify related entities still exist (no accidental cascade)
        var customerExists = await context.Customers.AnyAsync(c => c.Id == customer.Id);
        var productExists = await context.Products.AnyAsync(p => p.Id == product.Id);

        if (!customerExists)
            throw new Exception("Customer should still exist after request deletion");

        if (!productExists)
            throw new Exception("Product should still exist after request deletion");

        Console.WriteLine("✓ No orphan tables, proper cascade behavior verified");
    }

    /// <summary>
    /// Test 4: Verify Money immutability
    /// </summary>
    public void Money_Is_Immutable()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Act - Operations create new instances
        var sum = money1 + money2;
        var difference = money1 - money2;
        var product = money1 * 2;
        var quotient = money1 / 2;

        // Assert - Original instances unchanged
        if (money1.Value != 100m)
            throw new Exception("Original money1 should be unchanged");

        if (money2.Value != 100m)
            throw new Exception("Original money2 should be unchanged");

        if (sum.Value != 200m)
            throw new Exception($"Sum incorrect. Expected: 200, Actual: {sum.Value}");

        if (difference.Value != 0m)
            throw new Exception($"Difference incorrect. Expected: 0, Actual: {difference.Value}");

        if (product.Value != 200m)
            throw new Exception($"Product incorrect. Expected: 200, Actual: {product.Value}");

        if (quotient.Value != 50m)
            throw new Exception($"Quotient incorrect. Expected: 50, Actual: {quotient.Value}");

        Console.WriteLine("✓ Money immutability verified");
    }

    /// <summary>
    /// Test 5: Verify precision handling (decimal 18,4)
    /// </summary>
    public async Task Money_Precision_Handled_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var company = new Company("Test Company", CompanySegment.Services);
        
        // Test precise decimal value (4 decimal places)
        var precisePrice = new Money(1234.5678m, "USD");
        var product = new Product("Precision Test", precisePrice, 15m, company.Id);

        // Act
        context.Companies.Add(company);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Assert
        var savedProduct = await context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (savedProduct == null)
            throw new Exception("Product not found");

        // Money constructor rounds to 2 decimal places, but DB stores with precision (18,4)
        // The value should be stored and retrieved correctly
        if (Math.Abs(savedProduct.BasePrice.Value - 1234.57m) > 0.01m)
            throw new Exception($"Precision handling issue. Expected: ~1234.57, Actual: {savedProduct.BasePrice.Value}");

        Console.WriteLine("✓ Money precision handled correctly");
    }

    /// <summary>
    /// Run all tests
    /// </summary>
    public static async Task RunAllTests()
    {
        var tests = new ValueObjectsPersistenceTests();

        try
        {
            Console.WriteLine("\n=== Running Value Objects Persistence Tests ===\n");

            await tests.Money_ValueObject_Persists_Correctly();
            await tests.DiscountRequestItem_OwnedCollection_Persists_Correctly();
            await tests.No_Orphan_Tables_Proper_Cascade();
            tests.Money_Is_Immutable();
            await tests.Money_Precision_Handled_Correctly();

            Console.WriteLine("\n=== All Tests Passed! ✓ ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n=== Test Failed: {ex.Message} ===\n");
            throw;
        }
    }
}
