using Application.Ports;

namespace Infrastructure.Security;

/// <summary>
/// BCrypt-based implementation of password hashing.
/// Uses BCrypt.Net-Next library for secure password hashing with salt.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // Cost factor for BCrypt (higher = more secure but slower)

    /// <summary>
    /// Hashes a plain text password using BCrypt with automatic salt generation
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>BCrypt hashed password (includes salt)</returns>
    /// <exception cref="ArgumentNullException">Thrown when password is null</exception>
    /// <exception cref="ArgumentException">Thrown when password is empty or whitespace</exception>
    public string Hash(string password)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password), "Password cannot be null");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty or whitespace", nameof(password));

        // BCrypt.HashPassword automatically generates a salt and includes it in the hash
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies that a plain text password matches a BCrypt hashed password
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hashedPassword">Previously hashed password to compare against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when password or hashedPassword is null</exception>
    public bool Verify(string password, string hashedPassword)
    {
        if (password == null)
            throw new ArgumentNullException(nameof(password), "Password cannot be null");

        if (hashedPassword == null)
            throw new ArgumentNullException(nameof(hashedPassword), "Hashed password cannot be null");

        try
        {
            // BCrypt.Verify handles the comparison including the salt extraction from the hash
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Invalid hash format
            return false;
        }
    }
}
