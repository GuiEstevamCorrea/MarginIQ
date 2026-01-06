namespace MarginIQ.Application.DTOs.Auth;

/// <summary>
/// Request to authenticate a user with email and password.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Remember me for extended refresh token lifetime.
    /// </summary>
    public bool RememberMe { get; set; }
    
    /// <summary>
    /// Optional: Two-factor authentication code (if enabled).
    /// </summary>
    public string? TwoFactorCode { get; set; }
    
    /// <summary>
    /// Optional: Device identifier for security tracking.
    /// </summary>
    public string? DeviceId { get; set; }
    
    /// <summary>
    /// Optional: IP address for audit log.
    /// </summary>
    public string? IpAddress { get; set; }
}
