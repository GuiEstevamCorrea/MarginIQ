namespace MarginIQ.Application.DTOs.Auth;

/// <summary>
/// Request to reset a user's password using a reset token.
/// </summary>
public class PasswordResetRequest
{
    public Guid UserId { get; set; }
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request to change a user's password (requires current password).
/// </summary>
public class ChangePasswordRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
