using Infrastructure.Tests.Integration;

Console.WriteLine("MarginIQ - Value Objects Persistence Tests");
Console.WriteLine("==========================================\n");

try
{
    await ValueObjectsPersistenceTests.RunAllTests();
    
    Console.WriteLine("\n✓ All acceptance criteria met:");
    Console.WriteLine("  ✓ Data persisted correctly");
    Console.WriteLine("  ✓ No orphan tables");
    Console.WriteLine("  ✓ Queries load items correctly");
    Console.WriteLine("  ✓ Money immutability verified");
    Console.WriteLine("  ✓ Precision (decimal 18,4) handled correctly");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Tests failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
