using Infrastructure.Tests.Integration;

namespace Infrastructure.Tests;

/// <summary>
/// Test runner for value objects persistence verification
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("MarginIQ - Value Objects Persistence Tests");
        Console.WriteLine("==========================================");

        try
        {
            await ValueObjectsPersistenceTests.RunAllTests();
            Console.WriteLine("All acceptance criteria met:");
            Console.WriteLine("  ✓ Data persisted correctly");
            Console.WriteLine("  ✓ No orphan tables");
            Console.WriteLine("  ✓ Queries load items correctly");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
