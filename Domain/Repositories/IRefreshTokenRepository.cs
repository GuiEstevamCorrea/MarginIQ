using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for managing RefreshToken entities.
/// Handles storage and retrieval of JWT refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Adds a new refresh token to the repository.
    /// </summary>
    /// <param name="refreshToken">The refresh token to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a refresh token by its token value.
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token or null if not found</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="companyId">The company ID for multi-tenant isolation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active refresh tokens</returns>
    Task<List<RefreshToken>> GetActiveTokensByUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired refresh tokens from the database.
    /// Should be called periodically for cleanup.
    /// </summary>
    /// <param name="companyId">The company ID for multi-tenant isolation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens removed</returns>
    Task<int> RemoveExpiredTokensAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active refresh tokens for a user.
    /// Used for security incidents or password changes.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="companyId">The company ID for multi-tenant isolation</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(Guid userId, Guid companyId, string reason, CancellationToken cancellationToken = default);
}