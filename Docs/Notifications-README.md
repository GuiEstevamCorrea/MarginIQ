# Notification System

## Overview

This module implements the notification layer for MarginIQ, as specified in **Projeto.md section 7.2**.

The notification system ensures that all stakeholders are informed about important events in the discount approval workflow:
- **Salespersons** know when their requests are approved/rejected
- **Managers** know when approval is required
- **Admins** are notified about system events and governance changes

The implementation follows **hexagonal architecture** principles with async, non-blocking delivery.

## Supported Channels

### Phase 1 (Current)
1. **Email** (mandatory) - Primary notification channel

### Phase 2 (Future)
2. **WhatsApp** - Instant messaging for urgent notifications
3. **SMS** - Text messages for critical alerts

### Phase 3 (Future)
4. **Push Notifications** - Mobile app notifications
5. **In-App Notifications** - Dashboard notifications

## Architecture

```
Application
├── Ports
│   └── INotificationService.cs              // Port interface
│
└── UseCases
    └── SendDiscountRequestNotificationUseCase.cs    // Use case
```

Future Infrastructure adapters:
```
Infrastructure
└── Notifications
    ├── EmailAdapter.cs         // SMTP/SendGrid implementation
    ├── WhatsAppAdapter.cs      // WhatsApp Business API
    ├── SMSAdapter.cs           // Twilio/AWS SNS
    └── PushAdapter.cs          // Firebase Cloud Messaging
```

## Key Features

### 1. Multi-Channel Support
Send notifications through multiple channels simultaneously:
```csharp
Channels = new List<NotificationChannel> 
{ 
    NotificationChannel.Email, 
    NotificationChannel.WhatsApp 
}
```

### 2. Template-Based Notifications
Predefined templates ensure consistency and easy localization:
```csharp
Template = NotificationTemplate.DiscountRequestApproved
```

Templates automatically populate with context-specific data.

### 3. Priority Levels
- **Low**: Informational (daily digest, reports)
- **Normal**: Standard notifications (approval/rejection)
- **High**: Important actions required (adjustment needed)
- **Urgent**: Immediate action required (SLA expired)

### 4. Batch Operations
Send to multiple recipients efficiently:
```csharp
SendBatchAsync(new BatchNotificationRequest
{
    RecipientUserIds = managerIds,
    Template = NotificationTemplate.ApprovalRequired
});
```

### 5. Scheduled Notifications
Schedule notifications for future delivery:
```csharp
ScheduledFor = DateTime.UtcNow.AddHours(2)
```

### 6. Delivery Tracking
Track notification status across all channels:
- Sent
- Delivered
- Read/Opened
- Failed

### 7. Retry Logic
Automatic retry for failed deliveries with exponential backoff.

### 8. Audit Trail
All notifications are logged for compliance and troubleshooting.

## Port Interface: INotificationService

### Methods

#### SendAsync
Sends a custom notification (no template):
```csharp
var request = new NotificationRequest
{
    CompanyId = companyId,
    RecipientUserId = userId,
    RecipientEmail = "user@company.com",
    Channels = new List<NotificationChannel> { NotificationChannel.Email },
    Subject = "Discount Request Approved",
    Body = "Your discount request #12345 has been approved.",
    Priority = NotificationPriority.Normal
};

var result = await notificationService.SendAsync(request, cancellationToken);
```

#### SendFromTemplateAsync
Sends a notification using a predefined template:
```csharp
var request = new TemplateNotificationRequest
{
    CompanyId = companyId,
    RecipientUserId = userId,
    Template = NotificationTemplate.DiscountRequestApproved,
    Channels = new List<NotificationChannel> { NotificationChannel.Email },
    TemplateVariables = new Dictionary<string, string>
    {
        ["CustomerName"] = "Acme Corp",
        ["DiscountRequestId"] = "12345",
        ["RequestedDiscountPercentage"] = "15.50"
    },
    ActionUrl = "/discount-requests/12345"
};

var result = await notificationService.SendFromTemplateAsync(request, cancellationToken);
```

#### SendBatchAsync
Sends notifications to multiple recipients:
```csharp
var request = new BatchNotificationRequest
{
    CompanyId = companyId,
    RecipientUserIds = new List<Guid> { managerId1, managerId2, managerId3 },
    Template = NotificationTemplate.ApprovalRequired,
    Channels = new List<NotificationChannel> { NotificationChannel.Email },
    TemplateVariables = templateVars,
    Priority = NotificationPriority.High
};

var result = await notificationService.SendBatchAsync(request, cancellationToken);
```

#### GetStatusAsync
Retrieves notification delivery status:
```csharp
var status = await notificationService.GetStatusAsync(notificationId, cancellationToken);

if (status.ChannelStatuses[NotificationChannel.Email].Success)
{
    Console.WriteLine($"Email delivered at {status.DeliveredAt}");
}
```

#### GetHistoryAsync
Retrieves notification history for a company:
```csharp
var history = await notificationService.GetHistoryAsync(
    companyId,
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow,
    channel: NotificationChannel.Email,
    cancellationToken);

foreach (var notification in history)
{
    Console.WriteLine($"{notification.SentAt}: {notification.Subject} to {notification.RecipientName}");
}
```

#### IsChannelAvailableAsync
Checks if a channel is configured and healthy:
```csharp
var emailAvailable = await notificationService.IsChannelAvailableAsync(
    companyId,
    NotificationChannel.Email,
    cancellationToken);

if (!emailAvailable)
{
    // Fallback to alternative channel or skip
}
```

## Use Case: SendDiscountRequestNotificationUseCase

### Purpose
Orchestrates sending notifications for all discount request events.

### Supported Events

#### 1. DiscountRequestCreated
**Triggered**: When salesperson creates a new discount request  
**Recipients**: Managers and Admins  
**Template**: `ApprovalRequired`  
**Priority**: High  
**Content**: "New discount request from [Salesperson] for [Customer] requires your approval"

#### 2. DiscountRequestApproved
**Triggered**: When manager approves a discount request  
**Recipients**: Salesperson  
**Template**: `DiscountRequestApproved`  
**Priority**: Normal  
**Content**: "Your discount request for [Customer] has been approved"

#### 3. DiscountRequestRejected
**Triggered**: When manager rejects a discount request  
**Recipients**: Salesperson  
**Template**: `DiscountRequestRejected`  
**Priority**: Normal  
**Content**: "Your discount request for [Customer] has been rejected. Reason: [Justification]"

#### 4. DiscountRequestAdjustmentRequired
**Triggered**: When manager requests adjustments  
**Recipients**: Salesperson  
**Template**: `DiscountRequestAdjustmentRequired`  
**Priority**: High  
**Content**: "Please adjust your discount request for [Customer]. Details: [Justification]"

#### 5. DiscountRequestAutoApproved
**Triggered**: When AI auto-approves a discount request  
**Recipients**: Salesperson + Managers  
**Template**: `DiscountRequestAutoApproved`  
**Priority**: Normal  
**Content**: "Discount request for [Customer] was automatically approved by AI"

#### 6. SLAWarning
**Triggered**: When 80% of SLA time has elapsed  
**Recipients**: Managers  
**Template**: `SLAWarning`  
**Priority**: Urgent  
**Content**: "Discount request for [Customer] is approaching SLA deadline (created [X] hours ago)"

#### 7. SLAExpired
**Triggered**: When SLA time has been exceeded  
**Recipients**: Managers and Admins  
**Template**: `SLAExpired`  
**Priority**: Urgent  
**Content**: "SLA EXPIRED: Discount request for [Customer] requires immediate attention"

### Flow

1. **Validate**: Ensure discount request and company exist
2. **Load Context**: Get salesperson, customer, and company data
3. **Determine Recipients**: Based on event type (salesperson, managers, admins)
4. **Build Template Variables**: Customer name, discount %, margin, risk score, etc.
5. **Send Notifications**: Single or batch send depending on recipient count
6. **Log Audit**: Record notification event for compliance
7. **Return Results**: Success/failure status for each recipient

### Example Usage

```csharp
// When discount request is created
var request = new SendNotificationRequest
{
    DiscountRequestId = discountRequestId,
    EventType = DiscountRequestNotificationEvent.Created
};

var response = await useCase.ExecuteAsync(request, cancellationToken);

if (response.Success)
{
    Console.WriteLine($"Notified {response.SuccessfulRecipients} manager(s)");
}
```

```csharp
// When discount request is rejected with justification
var request = new SendNotificationRequest
{
    DiscountRequestId = discountRequestId,
    EventType = DiscountRequestNotificationEvent.Rejected,
    Justification = "Margin too low for this customer segment"
};

var response = await useCase.ExecuteAsync(request, cancellationToken);
```

## Notification Templates

### Template Variables

All templates have access to these variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `CustomerName` | Customer company name | "Acme Corp" |
| `SalespersonName` | Salesperson full name | "John Smith" |
| `DiscountRequestId` | Request ID (for tracking) | "abc123..." |
| `RequestedDiscountPercentage` | Discount % requested | "15.50" |
| `EstimatedMarginPercentage` | Estimated margin % | "22.30" |
| `RiskScore` | AI risk score (0-100) | "35" |
| `Status` | Current status | "UnderAnalysis" |
| `CreatedAt` | Creation timestamp | "2026-01-06 10:30" |
| `CompanyName` | Company name | "TechCorp Inc" |
| `TotalValue` | Total request value | "$25,450.00" |
| `Justification` | Reason (for reject/adjust) | "Margin too low" |

### Template Example: DiscountRequestApproved

**Subject**:
```
✅ Discount Request Approved - {{CustomerName}}
```

**Body (Plain Text)**:
```
Hello {{SalespersonName}},

Good news! Your discount request for {{CustomerName}} has been APPROVED.

Details:
- Discount: {{RequestedDiscountPercentage}}%
- Estimated Margin: {{EstimatedMarginPercentage}}%
- Total Value: {{TotalValue}}
- Risk Score: {{RiskScore}}

You can now proceed with closing the deal.

[View Details] → {{ActionUrl}}

Best regards,
{{CompanyName}} Team
```

**Body (HTML)**:
```html
<div style="font-family: Arial, sans-serif; max-width: 600px;">
  <h2 style="color: #10b981;">✅ Discount Request Approved</h2>
  
  <p>Hello <strong>{{SalespersonName}}</strong>,</p>
  
  <p>Good news! Your discount request for <strong>{{CustomerName}}</strong> has been <span style="color: #10b981;">APPROVED</span>.</p>
  
  <div style="background: #f3f4f6; padding: 16px; border-radius: 8px; margin: 16px 0;">
    <h3>Details</h3>
    <ul>
      <li><strong>Discount:</strong> {{RequestedDiscountPercentage}}%</li>
      <li><strong>Estimated Margin:</strong> {{EstimatedMarginPercentage}}%</li>
      <li><strong>Total Value:</strong> {{TotalValue}}</li>
      <li><strong>Risk Score:</strong> {{RiskScore}}</li>
    </ul>
  </div>
  
  <p>You can now proceed with closing the deal.</p>
  
  <a href="{{ActionUrl}}" style="background: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; margin: 16px 0;">
    View Details
  </a>
  
  <p style="color: #6b7280; font-size: 14px;">
    Best regards,<br>
    {{CompanyName}} Team
  </p>
</div>
```

## Integration Points

### From Use Cases

#### CreateDiscountRequestUseCase
After creating a discount request:
```csharp
// Send notification to managers
await _notificationUseCase.ExecuteAsync(new SendNotificationRequest
{
    DiscountRequestId = discountRequest.Id,
    EventType = DiscountRequestNotificationEvent.Created
}, cancellationToken);
```

#### ApproveOrRejectDiscountRequestUseCase
After approval/rejection:
```csharp
var eventType = decision == ApprovalDecision.Approve
    ? DiscountRequestNotificationEvent.Approved
    : DiscountRequestNotificationEvent.Rejected;

await _notificationUseCase.ExecuteAsync(new SendNotificationRequest
{
    DiscountRequestId = discountRequestId,
    EventType = eventType,
    Justification = justification
}, cancellationToken);
```

#### TryAutoApproveDiscountRequestUseCase
After AI auto-approval:
```csharp
await _notificationUseCase.ExecuteAsync(new SendNotificationRequest
{
    DiscountRequestId = discountRequest.Id,
    EventType = DiscountRequestNotificationEvent.AutoApproved
}, cancellationToken);
```

### Background Jobs (Future)

#### SLA Monitoring Job
Runs every 15 minutes to check for approaching/expired SLAs:
```csharp
public class SLAMonitoringJob
{
    public async Task ExecuteAsync()
    {
        var requests = await GetPendingRequestsAsync();
        
        foreach (var request in requests)
        {
            var elapsedHours = (DateTime.UtcNow - request.CreatedAt).TotalHours;
            var slaHours = 24; // configurable per company
            
            if (elapsedHours >= slaHours)
            {
                // SLA expired
                await SendNotificationAsync(
                    request.Id, 
                    DiscountRequestNotificationEvent.SLAExpired);
            }
            else if (elapsedHours >= slaHours * 0.8)
            {
                // SLA warning (80% elapsed)
                await SendNotificationAsync(
                    request.Id, 
                    DiscountRequestNotificationEvent.SLAWarning);
            }
        }
    }
}
```

## Future Implementation

### Phase 1: Email Adapter

```csharp
public class EmailNotificationAdapter : INotificationService
{
    private readonly IEmailProvider _emailProvider; // SendGrid, AWS SES, SMTP
    private readonly ITemplateEngine _templateEngine; // Razor, Liquid, Handlebars
    
    public async Task<NotificationResult> SendFromTemplateAsync(
        TemplateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Load template from database/files
        var template = await _templateEngine.LoadTemplateAsync(request.Template);
        
        // 2. Render template with variables
        var subject = _templateEngine.Render(template.Subject, request.TemplateVariables);
        var htmlBody = _templateEngine.Render(template.HtmlBody, request.TemplateVariables);
        var textBody = _templateEngine.Render(template.TextBody, request.TemplateVariables);
        
        // 3. Get recipient details
        var recipient = await _userRepository.GetByIdAsync(request.RecipientUserId);
        
        // 4. Send email
        var emailResult = await _emailProvider.SendEmailAsync(new EmailMessage
        {
            To = recipient.Email,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            Priority = MapPriority(request.Priority)
        });
        
        // 5. Save to notification history
        await SaveNotificationHistoryAsync(request, emailResult);
        
        // 6. Return result
        return new NotificationResult
        {
            Success = emailResult.Success,
            NotificationId = Guid.NewGuid(),
            ChannelResults = new Dictionary<NotificationChannel, ChannelDeliveryStatus>
            {
                [NotificationChannel.Email] = new ChannelDeliveryStatus
                {
                    Channel = NotificationChannel.Email,
                    Success = emailResult.Success,
                    Status = emailResult.Status,
                    ExternalId = emailResult.MessageId,
                    DeliveredAt = emailResult.DeliveredAt
                }
            }
        };
    }
}
```

### Phase 2: WhatsApp Adapter

```csharp
public class WhatsAppNotificationAdapter : INotificationService
{
    private readonly IWhatsAppBusinessClient _whatsAppClient;
    
    public async Task<NotificationResult> SendFromTemplateAsync(
        TemplateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        // WhatsApp requires pre-approved templates
        var templateName = MapToWhatsAppTemplate(request.Template);
        
        var recipient = await _userRepository.GetByIdAsync(request.RecipientUserId);
        
        var result = await _whatsAppClient.SendTemplateMessageAsync(new WhatsAppMessage
        {
            To = recipient.PhoneNumber,
            TemplateName = templateName,
            TemplateLanguage = "en_US",
            Parameters = request.TemplateVariables.Values.ToList()
        });
        
        return new NotificationResult
        {
            Success = result.Success,
            NotificationId = Guid.NewGuid(),
            ChannelResults = new Dictionary<NotificationChannel, ChannelDeliveryStatus>
            {
                [NotificationChannel.WhatsApp] = new ChannelDeliveryStatus
                {
                    Channel = NotificationChannel.WhatsApp,
                    Success = result.Success,
                    Status = result.Status,
                    ExternalId = result.MessageId,
                    DeliveredAt = result.SentAt
                }
            }
        };
    }
}
```

## Security & Privacy

### Email Security
- **SPF/DKIM/DMARC**: Proper email authentication to prevent spoofing
- **TLS Encryption**: All emails sent over encrypted connections
- **Unsubscribe**: Optional unsubscribe links for non-critical notifications

### Data Privacy
- **Minimal Data**: Only necessary information in notifications
- **No Sensitive Data**: Never include passwords, tokens, or financial details
- **Audit Trail**: All notifications logged for compliance (GDPR, LGPD)

### Rate Limiting
- **Per User**: Max 100 notifications per hour per user
- **Per Company**: Max 10,000 notifications per hour per company
- **Throttling**: Automatic throttling for burst scenarios

## Monitoring & Observability

### Metrics to Track
- Notification send rate (per channel, per template)
- Delivery success rate
- Average delivery time
- Open/read rates (for email)
- Bounce rates
- Failure rates by error type

### Logging
```csharp
_logger.LogInformation(
    "Sending notification: Template={Template}, Recipient={RecipientId}, Channels={Channels}",
    template, recipientId, string.Join(",", channels));

_logger.LogWarning(
    "Notification delivery failed: NotificationId={NotificationId}, Channel={Channel}, Error={Error}",
    notificationId, channel, error);
```

### Alerts
- Alert when delivery failure rate > 5%
- Alert when email bounce rate > 2%
- Alert when average delivery time > 30 seconds

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task SendNotification_WithValidRequest_ShouldSendSuccessfully()
{
    // Arrange: Mock notification service, repositories
    var mockNotificationService = new Mock<INotificationService>();
    mockNotificationService
        .Setup(s => s.SendFromTemplateAsync(It.IsAny<TemplateNotificationRequest>(), default))
        .ReturnsAsync(new NotificationResult { Success = true });
    
    // Act: Execute use case
    var response = await useCase.ExecuteAsync(request, default);
    
    // Assert: Verify notification sent
    Assert.True(response.Success);
    mockNotificationService.Verify(s => s.SendFromTemplateAsync(
        It.Is<TemplateNotificationRequest>(r => 
            r.Template == NotificationTemplate.DiscountRequestApproved),
        default), Times.Once);
}
```

### Integration Tests
```csharp
[Fact]
public async Task EmailAdapter_WithRealSMTP_ShouldDeliverEmail()
{
    // Use test SMTP server (MailHog, Papercut)
    var result = await emailAdapter.SendAsync(testRequest, default);
    
    Assert.True(result.Success);
    
    // Verify email received in test inbox
    var receivedEmail = await testInbox.GetLatestEmailAsync();
    Assert.Equal(expectedSubject, receivedEmail.Subject);
}
```

## Roadmap

### Phase 1: Foundation (Current)
- ✅ Port interface defined
- ✅ Notification templates enumerated
- ✅ Use case for discount request notifications
- ✅ Audit logging

### Phase 2: Email Implementation
- ⏳ Email adapter (SendGrid/AWS SES)
- ⏳ Template engine integration (Razor/Liquid)
- ⏳ Template management UI
- ⏳ Email delivery tracking

### Phase 3: WhatsApp Integration
- ⏳ WhatsApp Business API integration
- ⏳ Template approval workflow
- ⏳ Two-way messaging support
- ⏳ Media attachments

### Phase 4: Advanced Features
- ⏳ SMS adapter (Twilio/AWS SNS)
- ⏳ Push notifications (Firebase)
- ⏳ In-app notifications
- ⏳ Notification preferences per user
- ⏳ Digest mode (batch daily/weekly)
- ⏳ Smart scheduling (avoid nights/weekends)

### Phase 5: Intelligence
- ⏳ AI-powered send time optimization
- ⏳ Channel preference learning
- ⏳ Sentiment analysis for responses
- ⏳ A/B testing for templates

## Conclusion

This notification system provides a **solid foundation** for keeping all stakeholders informed throughout the discount approval workflow. The hexagonal architecture ensures:

1. **Testability**: Use cases can be tested without real email/SMS providers
2. **Flexibility**: Easy to add new channels (WhatsApp, SMS, Push)
3. **Maintainability**: Clear separation of concerns
4. **Scalability**: Async design supports high-volume notifications

The implementation aligns with **Projeto.md section 7.2** requirements:
- ✅ Email support (Phase 1)
- ✅ WhatsApp planned (Phase 2)
- ✅ Asynchronous architecture
- ✅ Template-based for consistency
- ✅ Multi-tenant isolation
- ✅ Audit trail for compliance

Next step: Implement Email adapter in Infrastructure layer to enable the **first working notification channel** for production use.
