using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by its unique identifier
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users</returns>
    Task<IEnumerable<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active users</returns>
    Task<IEnumerable<User>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users by role for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="role">User role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of users with the specified role</returns>
    Task<IEnumerable<User>> GetByRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all managers and admins for a specific company (users who can approve discounts)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvers</returns>
    Task<IEnumerable<User>> GetApproversAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user to the repository
    /// </summary>
    /// <param name="user">User to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">User to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user with the given email already exists in a company
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="excludeId">Optional user ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a user with the email exists in the company, false otherwise</returns>
    Task<bool> ExistsByEmailAsync(string email, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
