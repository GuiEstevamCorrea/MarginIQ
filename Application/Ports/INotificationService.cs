namespace Application.Ports;

/// <summary>
/// Port (interface) for notification services.
/// Follows hexagonal architecture: Application → Port → Adapter (Infrastructure).
/// 
/// Supported channels:
/// - Email (mandatory - Phase 1)
/// - WhatsApp (Phase 2)
/// - SMS (Phase 2)
/// - Push Notifications (Phase 3)
/// 
/// All notifications are:
/// - Asynchronous and non-blocking
/// - Multi-tenant isolated
/// - Template-based for consistency
/// - Auditable
/// - With retry logic for failures
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification through one or more channels
    /// </summary>
    /// <param name="request">Notification request with recipient, content, and channels</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with delivery status for each channel</returns>
    Task<NotificationResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification using a predefined template
    /// </summary>
    /// <param name="request">Template-based notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with delivery status for each channel</returns>
    Task<NotificationResult> SendFromTemplateAsync(
        TemplateNotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notifications to multiple recipients (batch)
    /// </summary>
    /// <param name="request">Batch notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results for each recipient</returns>
    Task<BatchNotificationResult> SendBatchAsync(
        BatchNotificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification delivery status
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Delivery status and details</returns>
    Task<NotificationStatus> GetStatusAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification history for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="startDate">Start date (optional)</param>
    /// <param name="endDate">End date (optional)</param>
    /// <param name="channel">Filter by channel (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sent notifications</returns>
    Task<List<NotificationHistory>> GetHistoryAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        NotificationChannel? channel = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a notification channel is available and configured
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="channel">Notification channel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if channel is configured and healthy</returns>
    Task<bool> IsChannelAvailableAsync(
        Guid companyId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates email addresses
    /// </summary>
    /// <param name="emailAddresses">List of email addresses to validate</param>
    /// <returns>Validation result for each email</returns>
    Task<Dictionary<string, bool>> ValidateEmailAddressesAsync(
        List<string> emailAddresses);
}

/// <summary>
/// Notification channels supported
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Email notification (mandatory - Phase 1)
    /// </summary>
    Email = 1,

    /// <summary>
    /// WhatsApp notification (Phase 2)
    /// </summary>
    WhatsApp = 2,

    /// <summary>
    /// SMS notification (Phase 2)
    /// </summary>
    SMS = 3,

    /// <summary>
    /// Push notification (Phase 3)
    /// </summary>
    Push = 4,

    /// <summary>
    /// In-app notification (Phase 3)
    /// </summary>
    InApp = 5
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Low priority - informational
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority - standard notifications
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority - important actions required
    /// </summary>
    High = 3,

    /// <summary>
    /// Urgent priority - immediate action required
    /// </summary>
    Urgent = 4
}

/// <summary>
/// Predefined notification templates
/// </summary>
public enum NotificationTemplate
{
    // Discount Request Events
    DiscountRequestCreated = 100,
    DiscountRequestApproved = 101,
    DiscountRequestRejected = 102,
    DiscountRequestAdjustmentRequired = 103,
    DiscountRequestAutoApproved = 104,
    DiscountRequestAutoApprovalReviewed = 105,

    // SLA Alerts
    SLAWarning = 200,
    SLAExpired = 201,

    // Manager Actions
    ApprovalRequired = 300,
    MultipleApprovalsRequired = 301,

    // AI Events
    AIAutoApprovalEnabled = 400,
    AIAutoApprovalDisabled = 401,
    AIModelRetrained = 402,
    AIGovernanceChanged = 403,

    // System Events
    WeeklyReport = 500,
    MonthlyReport = 501,
    DailyDigest = 502,

    // User Events
    WelcomeUser = 600,
    PasswordReset = 601,
    AccountActivated = 602,
    AccountDeactivated = 603
}

/// <summary>
/// Base notification request
/// </summary>
public class NotificationRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Recipient user ID
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Recipient email address (required for Email channel)
    /// </summary>
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// Recipient phone number (required for WhatsApp/SMS channels)
    /// </summary>
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// Notification channels to use
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Notification subject/title
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Notification body/content
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// HTML body for email (optional)
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Related entity type (e.g., "DiscountRequest", "Approval")
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Action URL (for email links, deep links)
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Action button text
    /// </summary>
    public string? ActionButtonText { get; set; }

    /// <summary>
    /// Schedule notification for later (optional)
    /// </summary>
    public DateTime? ScheduledFor { get; set; }
}

/// <summary>
/// Template-based notification request
/// </summary>
public class TemplateNotificationRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Recipient user ID
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Notification template to use
    /// </summary>
    public NotificationTemplate Template { get; set; }

    /// <summary>
    /// Notification channels to use
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Template variables (key-value pairs for placeholders)
    /// </summary>
    public Dictionary<string, string> TemplateVariables { get; set; } = new();

    /// <summary>
    /// Notification priority (overrides template default)
    /// </summary>
    public NotificationPriority? Priority { get; set; }

    /// <summary>
    /// Related entity type
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Action URL
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Schedule notification for later (optional)
    /// </summary>
    public DateTime? ScheduledFor { get; set; }
}

/// <summary>
/// Batch notification request (multiple recipients)
/// </summary>
public class BatchNotificationRequest
{
    /// <summary>
    /// Company ID (multi-tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// List of recipient user IDs
    /// </summary>
    public List<Guid> RecipientUserIds { get; set; } = new();

    /// <summary>
    /// Notification template to use
    /// </summary>
    public NotificationTemplate Template { get; set; }

    /// <summary>
    /// Notification channels to use
    /// </summary>
    public List<NotificationChannel> Channels { get; set; } = new();

    /// <summary>
    /// Template variables (same for all recipients)
    /// </summary>
    public Dictionary<string, string> TemplateVariables { get; set; } = new();

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Related entity type
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Related entity ID
    /// </summary>
    public Guid? EntityId { get; set; }
}

/// <summary>
/// Result of a notification send operation
/// </summary>
public class NotificationResult
{
    public bool Success { get; set; }
    public Guid NotificationId { get; set; }
    public Dictionary<NotificationChannel, ChannelDeliveryStatus> ChannelResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime SentAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
}

/// <summary>
/// Result of a batch notification operation
/// </summary>
public class BatchNotificationResult
{
    public bool Success { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessfulRecipients { get; set; }
    public int FailedRecipients { get; set; }
    public List<RecipientNotificationResult> RecipientResults { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime SentAt { get; set; }
}

/// <summary>
/// Result for a specific recipient in batch operation
/// </summary>
public class RecipientNotificationResult
{
    public Guid RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public bool Success { get; set; }
    public Guid? NotificationId { get; set; }
    public Dictionary<NotificationChannel, ChannelDeliveryStatus> ChannelResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Delivery status for a specific channel
/// </summary>
public class ChannelDeliveryStatus
{
    public NotificationChannel Channel { get; set; }
    public bool Success { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ExternalId { get; set; } // ID from email provider, WhatsApp API, etc.
}

/// <summary>
/// Notification status and tracking
/// </summary>
public class NotificationStatus
{
    public Guid NotificationId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public NotificationTemplate? Template { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Dictionary<NotificationChannel, ChannelDeliveryStatus> ChannelStatuses { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

/// <summary>
/// Notification history record
/// </summary>
public class NotificationHistory
{
    public Guid NotificationId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public NotificationTemplate? Template { get; set; }
    public string Subject { get; set; } = string.Empty;
    public List<NotificationChannel> Channels { get; set; } = new();
    public NotificationPriority Priority { get; set; }
    public bool Success { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string Status { get; set; } = string.Empty;
}
