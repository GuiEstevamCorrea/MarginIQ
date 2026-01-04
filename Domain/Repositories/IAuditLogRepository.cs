using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for AuditLog entity operations
/// Audit logs are immutable - no update or delete operations
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Gets an audit log by its unique identifier
    /// </summary>
    /// <param name="id">Audit log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit log if found, null otherwise</returns>
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs ordered by date descending</returns>
    Task<IEnumerable<AuditLog>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityName">Entity name</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the entity ordered by date descending</returns>
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by entity name for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="entityName">Entity name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the entity type</returns>
    Task<IEnumerable<AuditLog>> GetByEntityNameAsync(Guid companyId, string entityName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs for the user ordered by date descending</returns>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action type for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="action">Audit action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs with the specified action</returns>
    Task<IEnumerable<AuditLog>> GetByActionAsync(Guid companyId, AuditAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by origin (Human, AI, System) for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="origin">Audit origin</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs from the specified origin</returns>
    Task<IEnumerable<AuditLog>> GetByOriginAsync(Guid companyId, AuditOrigin origin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all AI-originated audit logs for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of AI audit logs</returns>
    Task<IEnumerable<AuditLog>> GetAILogsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all human-originated audit logs for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of human audit logs</returns>
    Task<IEnumerable<AuditLog>> GetHumanLogsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by date range for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit logs within the date range</returns>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent audit logs for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="count">Number of logs to retrieve (default 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent audit logs</returns>
    Task<IEnumerable<AuditLog>> GetRecentAsync(Guid companyId, int count = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit logs by entity name, action, or user
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching audit logs</returns>
    Task<IEnumerable<AuditLog>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new audit log entry to the repository
    /// </summary>
    /// <param name="auditLog">Audit log to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple audit log entries in batch
    /// </summary>
    /// <param name="auditLogs">Audit logs to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit log statistics for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with statistics (total, by origin, by action, etc.)</returns>
    Task<IDictionary<string, object>> GetStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of audit logs by origin for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with origin counts</returns>
    Task<IDictionary<AuditOrigin, int>> GetCountByOriginAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of audit logs by action for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary with action counts</returns>
    Task<IDictionary<AuditAction, int>> GetCountByActionAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives audit logs older than specified date (for data retention policies)
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="olderThan">Date threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of logs archived</returns>
    Task<int> ArchiveOldLogsAsync(Guid companyId, DateTime olderThan, CancellationToken cancellationToken = default);
}
