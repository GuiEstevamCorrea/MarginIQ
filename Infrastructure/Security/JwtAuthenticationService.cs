using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Ports;
using Domain.Entities;
using Infrastructure.Data;
using MarginIQ.Application.DTOs.Auth;
using MarginIQ.Application.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security;

/// <summary>
/// Implementation of JWT-based authentication service.
/// Handles token generation, validation, and refresh token management.
/// </summary>
public class JwtAuthenticationService : IAuthenticationService
{
    private readonly MarginIQDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecretKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public JwtAuthenticationService(
        MarginIQDbContext context, 
        IPasswordHasher passwordHasher, 
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;

        // JWT configuration from appsettings
        _jwtSecretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "MarginIQ";
        _jwtAudience = configuration["Jwt:Audience"] ?? "MarginIQ.Api";
        _accessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);
        _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower(), cancellationToken);

            if (user == null || user.PasswordHash == null)
            {
                return AuthenticationResult.Failed("Invalid email or password.");
            }

            // Verify password
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return AuthenticationResult.Failed("Invalid email or password.");
            }

            // Check if user is active
            if (user.Status != Domain.Enums.UserStatus.Active)
            {
                return AuthenticationResult.Failed("User account is not active.");
            }

            // Generate tokens
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
            var refreshTokenExpiresAt = request.RememberMe 
                ? DateTime.UtcNow.AddDays(_refreshTokenExpirationDays * 3) // Extended for "remember me"
                : DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            var accessToken = GenerateAccessToken(user, accessTokenExpiresAt);
            var refreshTokenValue = GenerateRefreshTokenValue();

            // Create and store refresh token
            var refreshToken = new RefreshToken(
                refreshTokenValue,
                user.Id,
                user.CompanyId,
                refreshTokenExpiresAt,
                request.DeviceId,
                request.IpAddress,
                null // UserAgent can be passed from controller if needed
            );

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            return AuthenticationResult.SuccessWithTokens(
                accessToken,
                refreshTokenValue,
                accessTokenExpiresAt,
                refreshTokenExpiresAt,
                user.Id,
                user.Name,
                user.Email,
                user.Role.ToString(),
                user.CompanyId,
                user.Company.Name
            );
        }
        catch (Exception ex)
        {
            // Log exception in real implementation
            return AuthenticationResult.Failed("An error occurred during authentication.");
        }
    }

    public async Task<TokenClaims?> ValidateAccessTokenAsync(string accessToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
                return null;

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty);
            var companyId = Guid.Parse(principal.FindFirst("CompanyId")?.Value ?? string.Empty);

            return new TokenClaims
            {
                UserId = userId,
                UserName = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                CompanyId = companyId,
                CompanyName = principal.FindFirst("CompanyName")?.Value ?? string.Empty,
                IssuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value)).DateTime,
                ExpiresAt = jwtToken.ValidTo,
                TokenId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Company)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (storedToken == null || !storedToken.IsValid())
            {
                return RefreshTokenResult.Failed("Invalid or expired refresh token.");
            }

            // Generate new access token
            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
            var accessToken = GenerateAccessToken(storedToken.User, accessTokenExpiresAt);

            // Optionally rotate refresh token (recommended for security)
            var newRefreshTokenValue = GenerateRefreshTokenValue();
            var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);

            // Revoke old refresh token
            storedToken.Revoke("Token rotation");

            // Create new refresh token
            var newRefreshToken = new RefreshToken(
                newRefreshTokenValue,
                storedToken.UserId,
                storedToken.CompanyId,
                newRefreshTokenExpiresAt,
                storedToken.DeviceId,
                storedToken.IpAddress,
                storedToken.UserAgent
            );

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync(cancellationToken);

            return RefreshTokenResult.SuccessWithTokens(
                accessToken,
                newRefreshTokenValue,
                accessTokenExpiresAt,
                newRefreshTokenExpiresAt
            );
        }
        catch (Exception ex)
        {
            // Log exception in real implementation
            return RefreshTokenResult.Failed("An error occurred during token refresh.");
        }
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.Revoke("Manual revocation");
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var token in userTokens)
        {
            token.Revoke("All tokens revoked");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public string HashPassword(string password)
    {
        return _passwordHasher.Hash(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return _passwordHasher.Verify(password, hashedPassword);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Generate a secure random token for password reset
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would store password reset tokens in the database
        // with expiration times and validate them here
        // For now, returning false as this is not implemented
        return false;
    }

    public async Task ResetPasswordAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        // Implementation would validate the reset token and update the user's password
        // Also revoke all existing refresh tokens for security
        throw new NotImplementedException("Password reset not implemented in this version.");
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null || user.PasswordHash == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is incorrect.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ArgumentException("New password and confirmation do not match.");
        }

        // Update password (this would need to be exposed in User entity)
        // user.UpdatePassword(HashPassword(request.NewPassword));

        // Revoke all existing refresh tokens for security
        await RevokeAllUserTokensAsync(user.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateTwoFactorCodeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Generate a 6-digit 2FA code
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public async Task<bool> ValidateTwoFactorCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would store and validate 2FA codes
        // For now, returning false as this is not implemented
        return false;
    }

    private string GenerateAccessToken(User user, DateTime expiresAt)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("CompanyId", user.CompanyId.ToString()),
            new Claim("CompanyName", user.Company?.Name ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshTokenValue()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}