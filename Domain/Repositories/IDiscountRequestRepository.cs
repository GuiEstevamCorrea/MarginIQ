using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for DiscountRequest entity operations
/// </summary>
public interface IDiscountRequestRepository
{
    /// <summary>
    /// Gets a discount request by its unique identifier
    /// </summary>
    /// <param name="id">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discount request if found, null otherwise</returns>
    Task<DiscountRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all discount requests for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests</returns>
    Task<IEnumerable<DiscountRequest>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests by status for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="status">Discount request status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests with the specified status</returns>
    Task<IEnumerable<DiscountRequest>> GetByStatusAsync(Guid companyId, DiscountRequestStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending discount requests (under analysis) for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending discount requests</returns>
    Task<IEnumerable<DiscountRequest>> GetPendingByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests by salesperson for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="salespersonId">Salesperson (User) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests created by the salesperson</returns>
    Task<IEnumerable<DiscountRequest>> GetBySalespersonIdAsync(Guid companyId, Guid salespersonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests by customer for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests for the customer</returns>
    Task<IEnumerable<DiscountRequest>> GetByCustomerIdAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets auto-approved discount requests (approved by AI) for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of auto-approved discount requests</returns>
    Task<IEnumerable<DiscountRequest>> GetAutoApprovedByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests with high risk score for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="threshold">Risk score threshold (default 70)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of high-risk discount requests</returns>
    Task<IEnumerable<DiscountRequest>> GetHighRiskByCompanyIdAsync(Guid companyId, decimal threshold = 70, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests by date range for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests within the date range</returns>
    Task<IEnumerable<DiscountRequest>> GetByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount requests that contain a specific product
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount requests containing the product</returns>
    Task<IEnumerable<DiscountRequest>> GetByProductIdAsync(Guid companyId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new discount request to the repository
    /// </summary>
    /// <param name="discountRequest">Discount request to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(DiscountRequest discountRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing discount request
    /// </summary>
    /// <param name="discountRequest">Discount request to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(DiscountRequest discountRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a discount request exists by ID
    /// </summary>
    /// <param name="id">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if discount request exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for discount requests in a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with statistics (total, approved, rejected, auto-approved, etc.)</returns>
    Task<IDictionary<string, int>> GetStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default);
}
