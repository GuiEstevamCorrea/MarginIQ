using MarginIQ.Application.DTOs.Auth;
using MarginIQ.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for authentication operations (login, logout, refresh tokens).
/// Implements JWT-based authentication with refresh token support.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns JWT access token and refresh token on success.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Add IP address and device info to request
            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.DeviceId = Request.Headers["X-Device-Id"].FirstOrDefault();

            var result = await _authenticationService.AuthenticateAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Set refresh token as httpOnly cookie for security
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.RefreshTokenExpiresAt
            };
            Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

            // Return access token and user info (but not refresh token in response body)
            return Ok(new
            {
                accessToken = result.AccessToken,
                expiresAt = result.AccessTokenExpiresAt,
                user = new
                {
                    id = result.UserId,
                    name = result.UserName,
                    email = result.UserEmail,
                    role = result.UserRole,
                    companyId = result.CompanyId,
                    companyName = result.CompanyName
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            // Get refresh token from httpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Refresh token not found." });
            }

            var request = new RefreshTokenRequest
            {
                RefreshToken = refreshToken
            };

            var result = await _authenticationService.RefreshTokenAsync(request, cancellationToken);

            if (!result.Success)
            {
                // Clear invalid refresh token cookie
                Response.Cookies.Delete("refreshToken");
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Update refresh token cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.RefreshTokenExpiresAt
            };
            Response.Cookies.Append("refreshToken", result.RefreshToken!, cookieOptions);

            return Ok(new
            {
                accessToken = result.AccessToken,
                expiresAt = result.AccessTokenExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Logs out the current user by revoking their refresh token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authenticationService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
            }

            // Clear refresh token cookie
            Response.Cookies.Delete("refreshToken");

            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Gets information about the current authenticated user.
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var companyId = User.FindFirst("CompanyId")?.Value;
            var companyName = User.FindFirst("CompanyName")?.Value;

            return Ok(new
            {
                id = userId,
                name = userName,
                email = userEmail,
                role = userRole,
                companyId = companyId,
                companyName = companyName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Validates the current access token.
    /// </summary>
    /// <returns>Token validation result</returns>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        // If we reach here, the token is valid (middleware validated it)
        return Ok(new { valid = true });
    }
}