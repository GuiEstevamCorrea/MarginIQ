using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for Customer entity operations
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Gets a customer by its unique identifier
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Customer if found, null otherwise</returns>
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customers</returns>
    Task<IEnumerable<Customer>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active customers for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active customers</returns>
    Task<IEnumerable<Customer>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by classification for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="classification">Customer classification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customers with the specified classification</returns>
    Task<IEnumerable<Customer>> GetByClassificationAsync(Guid companyId, CustomerClassification classification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by segment for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="segment">Business segment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customers in the specified segment</returns>
    Task<IEnumerable<Customer>> GetBySegmentAsync(Guid companyId, string segment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customers by status for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="status">Customer status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customers with the specified status</returns>
    Task<IEnumerable<Customer>> GetByStatusAsync(Guid companyId, CustomerStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by name for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="searchTerm">Search term to match against customer names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of customers matching the search term</returns>
    Task<IEnumerable<Customer>> SearchByNameAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new customer to the repository
    /// </summary>
    /// <param name="customer">Customer to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="customer">Customer to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer exists by ID
    /// </summary>
    /// <param name="id">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if customer exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer with the given name already exists in a company
    /// </summary>
    /// <param name="name">Customer name</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="excludeId">Optional customer ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a customer with the name exists in the company, false otherwise</returns>
    Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
