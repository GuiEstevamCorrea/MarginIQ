using Application.Ports;
using Infrastructure.Security;

namespace Infrastructure.Tests;

/// <summary>
/// Unit tests for password hashing functionality
/// </summary>
public class PasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new BCryptPasswordHasher();
    }

    public void TestPasswordHashing()
    {
        Console.WriteLine("Testing password hashing...");

        const string password = "MySecureP@ssw0rd!";

        // Hash the password
        var hashedPassword = _passwordHasher.Hash(password);
        Console.WriteLine($"✓ Password hashed successfully");
        Console.WriteLine($"  Original: {password}");
        Console.WriteLine($"  Hashed: {hashedPassword}");

        // Verify hash is not the same as original
        if (hashedPassword == password)
            throw new Exception("Hash should not be the same as original password");
        Console.WriteLine($"✓ Hash is different from original password");

        // Verify hash contains BCrypt prefix
        if (!hashedPassword.StartsWith("$2"))
            throw new Exception("Invalid BCrypt hash format");
        Console.WriteLine($"✓ Hash has valid BCrypt format");

        Console.WriteLine("Password hashing test passed!\n");
    }

    public void TestPasswordVerification()
    {
        Console.WriteLine("Testing password verification...");

        const string password = "MySecureP@ssw0rd!";
        const string wrongPassword = "WrongPassword123";

        // Hash the password
        var hashedPassword = _passwordHasher.Hash(password);

        // Verify correct password
        var isValid = _passwordHasher.Verify(password, hashedPassword);
        if (!isValid)
            throw new Exception("Correct password should verify successfully");
        Console.WriteLine($"✓ Correct password verified successfully");

        // Verify wrong password fails
        var isWrongValid = _passwordHasher.Verify(wrongPassword, hashedPassword);
        if (isWrongValid)
            throw new Exception("Wrong password should not verify");
        Console.WriteLine($"✓ Wrong password correctly rejected");

        Console.WriteLine("Password verification test passed!\n");
    }

    public void TestMultipleHashesAreDifferent()
    {
        Console.WriteLine("Testing that multiple hashes of same password are different (salt verification)...");

        const string password = "TestPassword123!";

        // Hash the same password twice
        var hash1 = _passwordHasher.Hash(password);
        var hash2 = _passwordHasher.Hash(password);

        // Hashes should be different due to different salts
        if (hash1 == hash2)
            throw new Exception("Multiple hashes of the same password should produce different results (different salts)");
        Console.WriteLine($"✓ Two hashes of same password are different (salt working)");
        Console.WriteLine($"  Hash 1: {hash1}");
        Console.WriteLine($"  Hash 2: {hash2}");

        // Both should verify correctly
        if (!_passwordHasher.Verify(password, hash1))
            throw new Exception("First hash should verify");
        if (!_passwordHasher.Verify(password, hash2))
            throw new Exception("Second hash should verify");
        Console.WriteLine($"✓ Both hashes verify correctly against original password");

        Console.WriteLine("Salt verification test passed!\n");
    }

    public void TestEmptyPasswordThrowsException()
    {
        Console.WriteLine("Testing empty password validation...");

        try
        {
            _passwordHasher.Hash("");
            throw new Exception("Hashing empty password should throw exception");
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"✓ Empty password correctly throws ArgumentException");
        }

        try
        {
            _passwordHasher.Hash("   ");
            throw new Exception("Hashing whitespace password should throw exception");
        }
        catch (ArgumentException)
        {
            Console.WriteLine($"✓ Whitespace password correctly throws ArgumentException");
        }

        Console.WriteLine("Empty password validation test passed!\n");
    }

    public void TestNullPasswordThrowsException()
    {
        Console.WriteLine("Testing null password validation...");

        try
        {
            _passwordHasher.Hash(null!);
            throw new Exception("Hashing null password should throw exception");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine($"✓ Null password correctly throws ArgumentNullException");
        }

        try
        {
            _passwordHasher.Verify(null!, "somehash");
            throw new Exception("Verifying null password should throw exception");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine($"✓ Null password in Verify correctly throws ArgumentNullException");
        }

        try
        {
            _passwordHasher.Verify("password", null!);
            throw new Exception("Verifying against null hash should throw exception");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine($"✓ Null hash in Verify correctly throws ArgumentNullException");
        }

        Console.WriteLine("Null password validation test passed!\n");
    }

    public void TestInvalidHashFormatReturnsFalse()
    {
        Console.WriteLine("Testing invalid hash format handling...");

        const string password = "TestPassword123!";
        const string invalidHash = "not-a-valid-bcrypt-hash";

        // Should return false instead of throwing exception
        var result = _passwordHasher.Verify(password, invalidHash);
        if (result)
            throw new Exception("Invalid hash should return false");
        Console.WriteLine($"✓ Invalid hash format correctly returns false");

        Console.WriteLine("Invalid hash format test passed!\n");
    }

    public void TestPasswordsNeverStoredInPlainText()
    {
        Console.WriteLine("Testing that passwords are never stored in plain text...");

        const string password = "SecretP@ssw0rd123!";
        var hashedPassword = _passwordHasher.Hash(password);

        // Verify the hash doesn't contain the original password
        if (hashedPassword.Contains(password))
            throw new Exception("Hash should not contain the original password");
        Console.WriteLine($"✓ Hash does not contain original password");

        // Verify the hash is significantly longer (BCrypt hashes are 60 chars)
        if (hashedPassword.Length < 50)
            throw new Exception("Hash should be at least 50 characters long");
        Console.WriteLine($"✓ Hash length is appropriate: {hashedPassword.Length} characters");

        Console.WriteLine("Plain text password test passed!\n");
    }

    public async Task RunAllTests()
    {
        try
        {
            TestPasswordHashing();
            TestPasswordVerification();
            TestMultipleHashesAreDifferent();
            TestEmptyPasswordThrowsException();
            TestNullPasswordThrowsException();
            TestInvalidHashFormatReturnsFalse();
            TestPasswordsNeverStoredInPlainText();

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("✓ ALL PASSWORD HASHER TESTS PASSED!");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("\nAcceptance Criteria Met:");
            Console.WriteLine("  ✓ Passwords never persisted in plain text");
            Console.WriteLine("  ✓ Hash validated correctly");
            Console.WriteLine("  ✓ Salt automatically generated per hash");
            Console.WriteLine("  ✓ Invalid inputs properly handled");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"✗ TEST FAILED: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.WriteLine("═══════════════════════════════════════");
            throw;
        }
    }

    public static async Task Main(string[] args)
    {
        var tests = new PasswordHasherTests();
        await tests.RunAllTests();
    }
}
