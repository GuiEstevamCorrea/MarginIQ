namespace Application.Ports;

/// <summary>
/// Port (interface) for external system integrations (ERP/CRM).
/// Follows hexagonal architecture: Application → Port → Adapter (Infrastructure).
/// 
/// Supported integrations (future):
/// - SAP
/// - TOTVS
/// - CSV/Excel imports
/// - Generic REST APIs
/// 
/// All integrations are:
/// - Asynchronous and decoupled
/// - Multi-tenant isolated
/// - With retry and error handling
/// - Auditable
/// </summary>
public interface IExternalSystemIntegrationService
{
    /// <summary>
    /// Checks if a specific integration type is available and configured for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="integrationType">Type of integration (SAP, TOTVS, CSV, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if integration is available and configured</returns>
    Task<bool> IsIntegrationAvailableAsync(
        Guid companyId,
        ExternalSystemType integrationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports customers from external system
    /// </summary>
    /// <param name="request">Customer import request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<ImportResult<CustomerImportData>> ImportCustomersAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports products from external system
    /// </summary>
    /// <param name="request">Product import request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<ImportResult<ProductImportData>> ImportProductsAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports users (salespersons) from external system
    /// </summary>
    /// <param name="request">User import request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with statistics</returns>
    Task<ImportResult<UserImportData>> ImportUsersAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports approved discount requests to external system
    /// For integration with ERP/CRM to close deals
    /// </summary>
    /// <param name="request">Export request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export result with statistics</returns>
    Task<ExportResult> ExportApprovedDiscountsAsync(
        ExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes data bidirectionally with external system
    /// </summary>
    /// <param name="request">Sync request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sync result with statistics</returns>
    Task<SyncResult> SyncDataAsync(
        SyncRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integration status and health
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="integrationType">Type of integration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Integration status</returns>
    Task<IntegrationStatus> GetIntegrationStatusAsync(
        Guid companyId,
        ExternalSystemType integrationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets integration logs for troubleshooting
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="startDate">Start date for logs</param>
    /// <param name="endDate">End date for logs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Integration logs</returns>
    Task<List<IntegrationLog>> GetIntegrationLogsAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// External system types supported
/// </summary>
public enum ExternalSystemType
{
    /// <summary>
    /// SAP ERP
    /// </summary>
    SAP = 1,

    /// <summary>
    /// TOTVS Protheus/RM
    /// </summary>
    TOTVS = 2,

    /// <summary>
    /// CSV/Excel file import
    /// </summary>
    CSV = 3,

    /// <summary>
    /// Salesforce CRM
    /// </summary>
    Salesforce = 4,

    /// <summary>
    /// Microsoft Dynamics
    /// </summary>
    Dynamics = 5,

    /// <summary>
    /// Generic REST API
    /// </summary>
    GenericAPI = 6,

    /// <summary>
    /// Oracle ERP
    /// </summary>
    Oracle = 7,

    /// <summary>
    /// Custom integration
    /// </summary>
    Custom = 99
}

/// <summary>
/// Import request for external data
/// </summary>
public class ImportRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Type of external system
    /// </summary>
    public ExternalSystemType SystemType { get; set; }

    /// <summary>
    /// Import mode: Full or Incremental
    /// </summary>
    public ImportMode Mode { get; set; } = ImportMode.Incremental;

    /// <summary>
    /// File path for CSV imports
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// File content as base64 for CSV imports (alternative to FilePath)
    /// </summary>
    public string? FileContentBase64 { get; set; }

    /// <summary>
    /// Connection string or API credentials (encrypted)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Additional configuration as JSON
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// User ID who initiated the import (for audit)
    /// </summary>
    public Guid InitiatedBy { get; set; }

    /// <summary>
    /// Filter criteria (e.g., modified since date)
    /// </summary>
    public DateTime? ModifiedSinceDate { get; set; }

    /// <summary>
    /// Dry run mode - validate without importing
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Import mode
/// </summary>
public enum ImportMode
{
    /// <summary>
    /// Full import - replaces all data
    /// </summary>
    Full = 1,

    /// <summary>
    /// Incremental import - only new/changed records
    /// </summary>
    Incremental = 2
}

/// <summary>
/// Export request for sending data to external system
/// </summary>
public class ExportRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Type of external system
    /// </summary>
    public ExternalSystemType SystemType { get; set; }

    /// <summary>
    /// Discount request IDs to export
    /// </summary>
    public List<Guid> DiscountRequestIds { get; set; } = new();

    /// <summary>
    /// Connection string or API credentials (encrypted)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Additional configuration as JSON
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// User ID who initiated the export (for audit)
    /// </summary>
    public Guid InitiatedBy { get; set; }

    /// <summary>
    /// Export format for file exports
    /// </summary>
    public ExportFormat Format { get; set; } = ExportFormat.JSON;
}

/// <summary>
/// Export format
/// </summary>
public enum ExportFormat
{
    JSON = 1,
    XML = 2,
    CSV = 3,
    Excel = 4
}

/// <summary>
/// Synchronization request
/// </summary>
public class SyncRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Type of external system
    /// </summary>
    public ExternalSystemType SystemType { get; set; }

    /// <summary>
    /// Entity types to sync
    /// </summary>
    public List<EntityType> EntitiesToSync { get; set; } = new();

    /// <summary>
    /// Sync direction
    /// </summary>
    public SyncDirection Direction { get; set; } = SyncDirection.Bidirectional;

    /// <summary>
    /// Connection string or API credentials (encrypted)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// User ID who initiated the sync (for audit)
    /// </summary>
    public Guid InitiatedBy { get; set; }

    /// <summary>
    /// Modified since date for incremental sync
    /// </summary>
    public DateTime? ModifiedSinceDate { get; set; }
}

/// <summary>
/// Entity types for sync
/// </summary>
public enum EntityType
{
    Customers = 1,
    Products = 2,
    Users = 3,
    DiscountRequests = 4,
    Approvals = 5
}

/// <summary>
/// Sync direction
/// </summary>
public enum SyncDirection
{
    /// <summary>
    /// Import from external system to MarginIQ
    /// </summary>
    Import = 1,

    /// <summary>
    /// Export from MarginIQ to external system
    /// </summary>
    Export = 2,

    /// <summary>
    /// Bidirectional sync
    /// </summary>
    Bidirectional = 3
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult<T> where T : class
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<T> ImportedData { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public string? JobId { get; set; }
}

/// <summary>
/// Result of an export operation
/// </summary>
public class ExportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int ExportedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ExportedFilePath { get; set; }
    public string? ExportedFileContentBase64 { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public string? JobId { get; set; }
}

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public Dictionary<EntityType, SyncEntityResult> EntityResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public string? JobId { get; set; }
}

/// <summary>
/// Sync result for a specific entity type
/// </summary>
public class SyncEntityResult
{
    public EntityType EntityType { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int ExportedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Integration status and health
/// </summary>
public class IntegrationStatus
{
    public Guid CompanyId { get; set; }
    public ExternalSystemType SystemType { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime? LastSyncDate { get; set; }
    public DateTime? NextScheduledSync { get; set; }
    public string? ConnectionStatus { get; set; }
    public List<string> ConfigurationErrors { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Integration log entry
/// </summary>
public class IntegrationLog
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public ExternalSystemType SystemType { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? ErrorDetails { get; set; }
    public DateTime Timestamp { get; set; }
    public int? RecordsProcessed { get; set; }
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Customer import data
/// </summary>
public class CustomerImportData
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Segment { get; set; }
    public string? Classification { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

/// <summary>
/// Product import data
/// </summary>
public class ProductImportData
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal BasePrice { get; set; }
    public decimal BaseMarginPercentage { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

/// <summary>
/// User import data
/// </summary>
public class UserImportData
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Salesperson";
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> CustomFields { get; set; } = new();
}
