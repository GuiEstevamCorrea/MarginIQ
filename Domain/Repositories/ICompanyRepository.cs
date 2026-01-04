using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for Company entity operations
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Gets a company by its unique identifier
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company if found, null otherwise</returns>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all companies with optional filtering
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of companies</returns>
    Task<IEnumerable<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active companies
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active companies</returns>
    Task<IEnumerable<Company>> GetActiveCompaniesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new company to the repository
    /// </summary>
    /// <param name="company">Company to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing company
    /// </summary>
    /// <param name="company">Company to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a company exists by ID
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if company exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a company with the given name already exists
    /// </summary>
    /// <param name="name">Company name</param>
    /// <param name="excludeId">Optional company ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a company with the name exists, false otherwise</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
