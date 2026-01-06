using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use case for sending notifications about discount request events.
/// 
/// This use case handles all notification scenarios for discount requests:
/// - Notify manager when new request is created
/// - Notify salesperson when request is approved/rejected
/// - Notify salesperson when auto-approved by AI
/// - Notify manager about SLA warnings
/// - Notify relevant parties about adjustment requests
/// 
/// Flow:
/// 1. Validate discount request and company
/// 2. Determine recipients based on event type
/// 3. Build notification from template with variables
/// 4. Send through configured channels (Email mandatory, WhatsApp Phase 2)
/// 5. Log notification for audit trail
/// </summary>
public class SendDiscountRequestNotificationUseCase
{
    private readonly INotificationService _notificationService;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public SendDiscountRequestNotificationUseCase(
        INotificationService notificationService,
        IDiscountRequestRepository discountRequestRepository,
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        ICompanyRepository companyRepository,
        IAuditLogRepository auditLogRepository)
    {
        _notificationService = notificationService;
        _discountRequestRepository = discountRequestRepository;
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _companyRepository = companyRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<SendNotificationResponse> ExecuteAsync(
        SendNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate discount request exists
        var discountRequest = await _discountRequestRepository.GetByIdAsync(
            request.DiscountRequestId,
            cancellationToken);

        if (discountRequest == null)
        {
            return new SendNotificationResponse
            {
                Success = false,
                ErrorMessage = "Discount request not found"
            };
        }

        // Step 2: Validate company exists and is active
        var company = await _companyRepository.GetByIdAsync(
            discountRequest.CompanyId,
            cancellationToken);

        if (company == null)
        {
            return new SendNotificationResponse
            {
                Success = false,
                ErrorMessage = "Company not found"
            };
        }

        if (company.Status != CompanyStatus.Active)
        {
            return new SendNotificationResponse
            {
                Success = false,
                ErrorMessage = "Company is not active"
            };
        }

        try
        {
            // Step 3: Load related entities for notification context
            var salesperson = await _userRepository.GetByIdAsync(
                discountRequest.SalespersonId,
                cancellationToken);

            var customer = await _customerRepository.GetByIdAsync(
                discountRequest.CustomerId,
                cancellationToken);

            if (salesperson == null || customer == null)
            {
                return new SendNotificationResponse
                {
                    Success = false,
                    ErrorMessage = "Related entities not found"
                };
            }

            // Step 4: Determine recipients and template based on event type
            var recipientIds = new List<Guid>();
            NotificationTemplate template;
            NotificationPriority priority;

            switch (request.EventType)
            {
                case DiscountRequestNotificationEvent.Created:
                    // Notify managers that approval is required
                    recipientIds = await GetManagerUserIdsAsync(discountRequest.CompanyId, cancellationToken);
                    template = NotificationTemplate.ApprovalRequired;
                    priority = NotificationPriority.High;
                    break;

                case DiscountRequestNotificationEvent.Approved:
                    // Notify salesperson of approval
                    recipientIds.Add(discountRequest.SalespersonId);
                    template = NotificationTemplate.DiscountRequestApproved;
                    priority = NotificationPriority.Normal;
                    break;

                case DiscountRequestNotificationEvent.Rejected:
                    // Notify salesperson of rejection
                    recipientIds.Add(discountRequest.SalespersonId);
                    template = NotificationTemplate.DiscountRequestRejected;
                    priority = NotificationPriority.Normal;
                    break;

                case DiscountRequestNotificationEvent.AdjustmentRequired:
                    // Notify salesperson that adjustment is needed
                    recipientIds.Add(discountRequest.SalespersonId);
                    template = NotificationTemplate.DiscountRequestAdjustmentRequired;
                    priority = NotificationPriority.High;
                    break;

                case DiscountRequestNotificationEvent.AutoApproved:
                    // Notify salesperson and managers of auto-approval
                    recipientIds.Add(discountRequest.SalespersonId);
                    recipientIds.AddRange(await GetManagerUserIdsAsync(discountRequest.CompanyId, cancellationToken));
                    template = NotificationTemplate.DiscountRequestAutoApproved;
                    priority = NotificationPriority.Normal;
                    break;

                case DiscountRequestNotificationEvent.SLAWarning:
                    // Notify managers that SLA is approaching
                    recipientIds = await GetManagerUserIdsAsync(discountRequest.CompanyId, cancellationToken);
                    template = NotificationTemplate.SLAWarning;
                    priority = NotificationPriority.Urgent;
                    break;

                case DiscountRequestNotificationEvent.SLAExpired:
                    // Notify managers that SLA has expired
                    recipientIds = await GetManagerUserIdsAsync(discountRequest.CompanyId, cancellationToken);
                    template = NotificationTemplate.SLAExpired;
                    priority = NotificationPriority.Urgent;
                    break;

                default:
                    return new SendNotificationResponse
                    {
                        Success = false,
                        ErrorMessage = $"Unknown event type: {request.EventType}"
                    };
            }

            // Remove duplicates
            recipientIds = recipientIds.Distinct().ToList();

            if (recipientIds.Count == 0)
            {
                return new SendNotificationResponse
                {
                    Success = false,
                    ErrorMessage = "No recipients found for notification"
                };
            }

            // Step 5: Build template variables
            var templateVariables = new Dictionary<string, string>
            {
                ["CustomerName"] = customer.Name,
                ["SalespersonName"] = salesperson.Name,
                ["DiscountRequestId"] = discountRequest.Id.ToString(),
                ["RequestedDiscountPercentage"] = discountRequest.RequestedDiscountPercentage.ToString("F2"),
                ["EstimatedMarginPercentage"] = (discountRequest.EstimatedMarginPercentage ?? 0).ToString("F2"),
                ["RiskScore"] = discountRequest.RiskScore?.ToString("F0") ?? "N/A",
                ["Status"] = discountRequest.Status.ToString(),
                ["CreatedAt"] = discountRequest.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                ["CompanyName"] = company.Name
            };

            // Add total value
            var totalValue = discountRequest.Items.Sum(i => i.GetTotalFinalPrice().Value);
            templateVariables["TotalValue"] = totalValue.ToString("C2");

            // Add justification if available (for rejections/adjustments)
            if (!string.IsNullOrEmpty(request.Justification))
            {
                templateVariables["Justification"] = request.Justification;
            }

            // Add action URL (deep link to discount request details)
            var actionUrl = $"/discount-requests/{discountRequest.Id}";

            // Step 6: Send notifications to all recipients
            var notificationResults = new List<RecipientNotificationResult>();

            if (recipientIds.Count == 1)
            {
                // Single recipient - use regular send
                var recipient = await _userRepository.GetByIdAsync(recipientIds[0], cancellationToken);
                if (recipient != null)
                {
                    var notificationRequest = new TemplateNotificationRequest
                    {
                        CompanyId = discountRequest.CompanyId,
                        RecipientUserId = recipient.Id,
                        Template = template,
                        Channels = new List<NotificationChannel> { NotificationChannel.Email },
                        TemplateVariables = templateVariables,
                        Priority = priority,
                        EntityType = "DiscountRequest",
                        EntityId = discountRequest.Id,
                        ActionUrl = actionUrl
                    };

                    var result = await _notificationService.SendFromTemplateAsync(
                        notificationRequest,
                        cancellationToken);

                    notificationResults.Add(new RecipientNotificationResult
                    {
                        RecipientUserId = recipient.Id,
                        RecipientEmail = recipient.Email,
                        Success = result.Success,
                        NotificationId = result.NotificationId,
                        ChannelResults = result.ChannelResults,
                        ErrorMessage = result.Errors.Any() ? string.Join(", ", result.Errors) : null
                    });
                }
            }
            else
            {
                // Multiple recipients - use batch send
                var batchRequest = new BatchNotificationRequest
                {
                    CompanyId = discountRequest.CompanyId,
                    RecipientUserIds = recipientIds,
                    Template = template,
                    Channels = new List<NotificationChannel> { NotificationChannel.Email },
                    TemplateVariables = templateVariables,
                    Priority = priority,
                    EntityType = "DiscountRequest",
                    EntityId = discountRequest.Id
                };

                var batchResult = await _notificationService.SendBatchAsync(
                    batchRequest,
                    cancellationToken);

                notificationResults.AddRange(batchResult.RecipientResults);
            }

            // Step 7: Log notification event for audit
            await LogNotificationEventAsync(
                discountRequest.CompanyId,
                discountRequest.Id,
                request.EventType,
                recipientIds,
                notificationResults.All(r => r.Success),
                cancellationToken);

            // Step 8: Return results
            var successCount = notificationResults.Count(r => r.Success);
            var failedCount = notificationResults.Count(r => !r.Success);

            return new SendNotificationResponse
            {
                Success = successCount > 0,
                Message = $"Sent {successCount} notification(s) successfully, {failedCount} failed",
                TotalRecipients = notificationResults.Count,
                SuccessfulRecipients = successCount,
                FailedRecipients = failedCount,
                NotificationResults = notificationResults
            };
        }
        catch (Exception ex)
        {
            // Log error
            await LogNotificationEventAsync(
                discountRequest.CompanyId,
                discountRequest.Id,
                request.EventType,
                new List<Guid>(),
                false,
                cancellationToken);

            return new SendNotificationResponse
            {
                Success = false,
                ErrorMessage = $"Failed to send notification: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets all manager and admin user IDs for a company
    /// </summary>
    private async Task<List<Guid>> GetManagerUserIdsAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        
        return users
            .Where(u => u.Role == UserRole.Manager || u.Role == UserRole.Admin)
            .Where(u => u.Status == UserStatus.Active)
            .Select(u => u.Id)
            .ToList();
    }

    /// <summary>
    /// Logs notification event to audit log
    /// </summary>
    private async Task LogNotificationEventAsync(
        Guid companyId,
        Guid discountRequestId,
        DiscountRequestNotificationEvent eventType,
        List<Guid> recipientIds,
        bool success,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                EventType = eventType.ToString(),
                RecipientCount = recipientIds.Count,
                RecipientUserIds = recipientIds,
                Success = success
            });

            var auditLog = AuditLog.CreateForSystem(
                "DiscountRequest",
                discountRequestId,
                AuditAction.Other,
                companyId,
                payload);

            await _auditLogRepository.AddAsync(auditLog);
        }
        catch
        {
            // Audit log failure should not break notification
        }
    }
}

/// <summary>
/// Notification events for discount requests
/// </summary>
public enum DiscountRequestNotificationEvent
{
    Created = 1,
    Approved = 2,
    Rejected = 3,
    AdjustmentRequired = 4,
    AutoApproved = 5,
    SLAWarning = 6,
    SLAExpired = 7
}

/// <summary>
/// Request to send discount request notification
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Discount request ID
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// Type of notification event
    /// </summary>
    public DiscountRequestNotificationEvent EventType { get; set; }

    /// <summary>
    /// Optional justification (for rejections/adjustments)
    /// </summary>
    public string? Justification { get; set; }
}

/// <summary>
/// Response for send notification operation
/// </summary>
public class SendNotificationResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessfulRecipients { get; set; }
    public int FailedRecipients { get; set; }
    public List<RecipientNotificationResult> NotificationResults { get; set; } = new();
}
