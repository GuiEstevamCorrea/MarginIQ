using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for Approval entity operations
/// </summary>
public interface IApprovalRepository
{
    /// <summary>
    /// Gets an approval by its unique identifier
    /// </summary>
    /// <param name="id">Approval ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approval if found, null otherwise</returns>
    Task<Approval?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals by discount request ID
    /// </summary>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals for the discount request</returns>
    Task<IEnumerable<Approval>> GetByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest approval for a discount request
    /// </summary>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Latest approval if found, null otherwise</returns>
    Task<Approval?> GetLatestByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals by approver (user) ID
    /// </summary>
    /// <param name="approverId">Approver (User) ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals made by the approver</returns>
    Task<IEnumerable<Approval>> GetByApproverIdAsync(Guid approverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals by decision type
    /// </summary>
    /// <param name="decision">Approval decision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals with the specified decision</returns>
    Task<IEnumerable<Approval>> GetByDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals by source (Human or AI)
    /// </summary>
    /// <param name="source">Approval source</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals from the specified source</returns>
    Task<IEnumerable<Approval>> GetBySourceAsync(ApprovalSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI approvals
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of AI approvals</returns>
    Task<IEnumerable<Approval>> GetAIApprovalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals by date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals within the date range</returns>
    Task<IEnumerable<Approval>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals that exceeded SLA threshold
    /// </summary>
    /// <param name="slaThresholdInSeconds">SLA threshold in seconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals that exceeded SLA</returns>
    Task<IEnumerable<Approval>> GetExceededSlaAsync(int slaThresholdInSeconds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approvals for a company (via discount request)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of approvals for the company</returns>
    Task<IEnumerable<Approval>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new approval to the repository
    /// </summary>
    /// <param name="approval">Approval to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Approval approval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing approval
    /// </summary>
    /// <param name="approval">Approval to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(Approval approval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an approval exists by ID
    /// </summary>
    /// <param name="id">Approval ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if approval exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics for approvals (total by decision, by source, average SLA, etc.)
    /// </summary>
    /// <param name="companyId">Optional company ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with approval statistics</returns>
    Task<IDictionary<string, object>> GetStatisticsAsync(Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average SLA time for approvals
    /// </summary>
    /// <param name="companyId">Optional company ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Average SLA time in seconds</returns>
    Task<double> GetAverageSlaTimeAsync(Guid? companyId = null, CancellationToken cancellationToken = default);
}
