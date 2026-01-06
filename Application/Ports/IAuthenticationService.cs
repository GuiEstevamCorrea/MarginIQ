using MarginIQ.Application.DTOs.Auth;

namespace MarginIQ.Application.Ports;

/// <summary>
/// Authentication service port for JWT token generation, validation, and user authentication.
/// Implements security requirements from Projeto.md section 8.2.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns JWT tokens (access + refresh) on success.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens and user info</returns>
    Task<AuthenticationResult> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT access token.
    /// Returns token claims if valid, null if invalid/expired.
    /// </summary>
    /// <param name="accessToken">JWT access token</param>
    /// <returns>Token claims or null</returns>
    Task<TokenClaims?> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// Returns new access token and optionally a new refresh token.
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New tokens</returns>
    Task<RefreshTokenResult> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token to invalidate it.
    /// Used for logout and security incidents.
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// Used for password reset and security incidents.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hashes a password using a secure algorithm (bcrypt, Argon2, PBKDF2).
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its hash.
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <param name="hashedPassword">Hashed password</param>
    /// <returns>True if password matches</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Generates a secure password reset token.
    /// Token should expire in 1-2 hours.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset token</returns>
    Task<string> GeneratePasswordResetTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password reset token.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="token">Reset token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if token is valid and not expired</returns>
    Task<bool> ValidatePasswordResetTokenAsync(
        Guid userId,
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// Revokes all existing refresh tokens for security.
    /// </summary>
    /// <param name="request">Password reset request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetPasswordAsync(
        PasswordResetRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes a user's password (requires current password).
    /// Revokes all existing refresh tokens for security.
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a two-factor authentication code for a user.
    /// Used for sensitive operations (optional for MVP).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>2FA code (6 digits)</returns>
    Task<string> GenerateTwoFactorCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a two-factor authentication code.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="code">2FA code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code is valid</returns>
    Task<bool> ValidateTwoFactorCodeAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of authentication (login) operation.
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Tokens
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    
    // User info
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    // Security
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresPasswordChange { get; set; }
    
    public static AuthenticationResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
        
    public static AuthenticationResult SuccessWithTokens(
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresAt,
        DateTime refreshTokenExpiresAt,
        Guid userId,
        string userName,
        string userEmail,
        string userRole,
        Guid companyId,
        string companyName) =>
        new()
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            UserRole = userRole,
            CompanyId = companyId,
            CompanyName = companyName
        };
}

/// <summary>
/// Claims extracted from a JWT token.
/// </summary>
public class TokenClaims
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenId { get; set; } = string.Empty; // jti claim for revocation
}

/// <summary>
/// Result of token refresh operation.
/// </summary>
public class RefreshTokenResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    
    public static RefreshTokenResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
        
    public static RefreshTokenResult SuccessWithTokens(
        string accessToken,
        string refreshToken,
        DateTime accessTokenExpiresAt,
        DateTime refreshTokenExpiresAt) =>
        new()
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
}
