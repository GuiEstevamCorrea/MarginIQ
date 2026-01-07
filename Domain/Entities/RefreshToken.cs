using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// Refresh tokens are used to obtain new access tokens without re-authentication.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier for the refresh token
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The actual refresh token value (should be cryptographically secure)
    /// </summary>
    public string Token { get; private set; }

    /// <summary>
    /// User ID this refresh token belongs to
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Company ID for multi-tenant isolation
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// When the refresh token was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the refresh token expires
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Whether the refresh token is still active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When the refresh token was revoked (if applicable)
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation (logout, security, password change, etc.)
    /// </summary>
    public string? RevocationReason { get; private set; }

    /// <summary>
    /// Device identifier for security tracking
    /// </summary>
    public string? DeviceId { get; private set; }

    /// <summary>
    /// IP address when token was created
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent when token was created
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Navigation property to Company
    /// </summary>
    public Company Company { get; private set; } = null!;

    /// <summary>
    /// Private parameterless constructor for EF Core
    /// </summary>
    private RefreshToken()
    {
        Token = string.Empty;
    }

    /// <summary>
    /// Creates a new refresh token
    /// </summary>
    public RefreshToken(
        string token,
        Guid userId,
        Guid companyId,
        DateTime expiresAt,
        string? deviceId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Id = Guid.NewGuid();
        Token = token ?? throw new ArgumentNullException(nameof(token));
        UserId = userId;
        CompanyId = companyId;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
        IsActive = true;
        DeviceId = deviceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// Revokes the refresh token
    /// </summary>
    public void Revoke(string reason)
    {
        if (!IsActive)
            return;

        IsActive = false;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
    }

    /// <summary>
    /// Checks if the refresh token is valid (active and not expired)
    /// </summary>
    public bool IsValid()
    {
        return IsActive && DateTime.UtcNow <= ExpiresAt;
    }
}