namespace MarginIQ.Application.DTOs.Auth;

/// <summary>
/// Request to refresh an expired access token.
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Expired access token for validation.
    /// Some implementations validate the expired token to ensure it matches the refresh token.
    /// </summary>
    public string? ExpiredAccessToken { get; set; }
}
