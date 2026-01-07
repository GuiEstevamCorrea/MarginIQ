namespace Application.Ports;

/// <summary>
/// Interface for password hashing operations.
/// Provides secure password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain text password using a secure hashing algorithm
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>Hashed password that can be safely stored</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies that a plain text password matches a hashed password
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hashedPassword">Previously hashed password to compare against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool Verify(string password, string hashedPassword);
}
