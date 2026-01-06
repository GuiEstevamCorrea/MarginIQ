using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case UC-02: Approve or Reject Discount Request
/// 
/// Allows managers to approve or reject discount requests.
/// Business rules:
/// - Justification is mandatory when rejecting
/// - Records SLA (time from request creation to decision)
/// - Updates request status based on decision
/// - Creates audit log for traceability
/// - Optionally stores learning data for AI training
/// </summary>
public class ApproveOrRejectDiscountRequestUseCase
{
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAILearningDataRepository _aiLearningDataRepository;
    private readonly IAIService _aiService;

    public ApproveOrRejectDiscountRequestUseCase(
        IDiscountRequestRepository discountRequestRepository,
        IApprovalRepository approvalRepository,
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IAuditLogRepository auditLogRepository,
        IAILearningDataRepository aiLearningDataRepository,
        IAIService aiService)
    {
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _approvalRepository = approvalRepository ?? throw new ArgumentNullException(nameof(approvalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _aiLearningDataRepository = aiLearningDataRepository ?? throw new ArgumentNullException(nameof(aiLearningDataRepository));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
    }

    /// <summary>
    /// Executes the approve or reject discount request use case
    /// </summary>
    public async Task<ApproveOrRejectDiscountRequestResponse> ExecuteAsync(
        ApproveOrRejectDiscountRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Step 1: Validate and load context
        var context = await ValidateAndLoadContextAsync(request, cancellationToken);

        // Step 2: Validate business rules
        ValidateBusinessRules(request, context.DiscountRequest);

        // Step 3: Calculate SLA
        var slaTimeInSeconds = CalculateSlaTime(context.DiscountRequest);

        // Step 4: Create approval record
        var approval = new Approval(
            discountRequestId: request.DiscountRequestId,
            approverId: request.ApproverId,
            decision: request.Decision,
            slaTimeInSeconds: slaTimeInSeconds,
            justification: request.Justification,
            metadata: request.Metadata);

        await _approvalRepository.AddAsync(approval, cancellationToken);

        // Step 5: Update discount request status
        UpdateDiscountRequestStatus(context.DiscountRequest, request.Decision);

        await _discountRequestRepository.UpdateAsync(context.DiscountRequest, cancellationToken);

        // Step 6: Create audit log
        await CreateAuditLogAsync(
            context.DiscountRequest,
            context.Approver,
            request.Decision,
            approval.Id,
            cancellationToken);

        // Step 7: Update AI learning data (if exists)
        await UpdateLearningDataAsync(
            context.DiscountRequest,
            request.Decision,
            cancellationToken);

        // Step 8: Build and return response
        return BuildResponse(
            context.DiscountRequest,
            approval,
            context.Approver,
            context.Customer,
            slaTimeInSeconds);
    }

    /// <summary>
    /// Validates request and loads all required entities
    /// </summary>
    private async Task<ValidationContext> ValidateAndLoadContextAsync(
        ApproveOrRejectDiscountRequestRequest request,
        CancellationToken cancellationToken)
    {
        // Load discount request
        var discountRequest = await _discountRequestRepository.GetByIdAsync(request.DiscountRequestId, cancellationToken);
        if (discountRequest == null)
            throw new InvalidOperationException($"Discount request with ID {request.DiscountRequestId} not found");

        // Validate multi-tenant isolation
        if (discountRequest.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Discount request does not belong to the specified company");

        // Load approver
        var approver = await _userRepository.GetByIdAsync(request.ApproverId, cancellationToken);
        if (approver == null)
            throw new InvalidOperationException($"Approver with ID {request.ApproverId} not found");

        // Validate approver belongs to company
        if (approver.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Approver does not belong to the specified company");

        // Validate approver is active
        if (approver.Status != UserStatus.Active)
            throw new InvalidOperationException($"Approver is not active (status: {approver.Status})");

        // Validate approver has permission (Manager or Admin)
        if (approver.Role != UserRole.Manager && approver.Role != UserRole.Admin)
            throw new InvalidOperationException($"User does not have permission to approve requests. Required role: Manager or Admin. Current role: {approver.Role}");

        // Load customer (for response context)
        var customer = await _customerRepository.GetByIdAsync(discountRequest.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {discountRequest.CustomerId} not found");

        return new ValidationContext
        {
            DiscountRequest = discountRequest,
            Approver = approver,
            Customer = customer
        };
    }

    /// <summary>
    /// Validates business rules for approval/rejection
    /// </summary>
    private void ValidateBusinessRules(
        ApproveOrRejectDiscountRequestRequest request,
        DiscountRequest discountRequest)
    {
        // Rule 1: Request must be in "Under Analysis" status
        if (discountRequest.Status != DiscountRequestStatus.UnderAnalysis)
        {
            throw new InvalidOperationException(
                $"Cannot approve/reject request with status {discountRequest.Status}. " +
                $"Only requests with status 'UnderAnalysis' can be approved or rejected.");
        }

        // Rule 2: Justification is mandatory when rejecting
        if (request.Decision == ApprovalDecision.Reject && string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new InvalidOperationException("Justification is mandatory when rejecting a discount request");
        }

        // Rule 3: Justification is mandatory when requesting adjustment
        if (request.Decision == ApprovalDecision.RequestAdjustment && string.IsNullOrWhiteSpace(request.Justification))
        {
            throw new InvalidOperationException("Justification is mandatory when requesting adjustment to a discount request");
        }
    }

    /// <summary>
    /// Calculates SLA time in seconds from request creation to decision
    /// </summary>
    private int CalculateSlaTime(DiscountRequest discountRequest)
    {
        var now = DateTime.UtcNow;
        var timeSpan = now - discountRequest.CreatedAt;
        return (int)timeSpan.TotalSeconds;
    }

    /// <summary>
    /// Updates discount request status based on approval decision
    /// </summary>
    private void UpdateDiscountRequestStatus(DiscountRequest discountRequest, ApprovalDecision decision)
    {
        switch (decision)
        {
            case ApprovalDecision.Approve:
                discountRequest.Approve();
                break;

            case ApprovalDecision.Reject:
                discountRequest.Reject();
                break;

            case ApprovalDecision.RequestAdjustment:
                discountRequest.RequestAdjustment();
                break;

            default:
                throw new InvalidOperationException($"Unknown approval decision: {decision}");
        }
    }

    /// <summary>
    /// Creates audit log for the approval/rejection action
    /// </summary>
    private async Task CreateAuditLogAsync(
        DiscountRequest discountRequest,
        User approver,
        ApprovalDecision decision,
        Guid approvalId,
        CancellationToken cancellationToken)
    {
        var action = decision switch
        {
            ApprovalDecision.Approve => AuditAction.Approved,
            ApprovalDecision.Reject => AuditAction.Rejected,
            ApprovalDecision.RequestAdjustment => AuditAction.StatusChanged,
            _ => AuditAction.Other
        };

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            discountRequestId = discountRequest.Id,
            approvalId = approvalId,
            decision = decision.ToString(),
            approverId = approver.Id,
            approverName = approver.Name,
            approverRole = approver.Role.ToString(),
            customerId = discountRequest.CustomerId,
            requestedDiscount = discountRequest.RequestedDiscountPercentage,
            estimatedMargin = discountRequest.EstimatedMarginPercentage,
            riskScore = discountRequest.RiskScore
        });

        var auditLog = AuditLog.CreateForHuman(
            entityName: nameof(DiscountRequest),
            entityId: discountRequest.Id,
            action: action,
            companyId: discountRequest.CompanyId,
            userId: approver.Id,
            payload: payload);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Updates AI learning data with the human decision
    /// This allows the AI to learn from human decisions
    /// </summary>
    private async Task UpdateLearningDataAsync(
        DiscountRequest discountRequest,
        ApprovalDecision decision,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to find existing learning data for this request
            var learningData = await _aiLearningDataRepository.GetByDiscountRequestIdAsync(
                discountRequest.Id,
                cancellationToken);

            if (learningData != null)
            {
                // Update with human decision
                var approved = decision == ApprovalDecision.Approve;
                var approvedDiscount = approved ? discountRequest.RequestedDiscountPercentage : (decimal?)null;
                
                learningData.RecordHumanDecision(approved, approvedDiscount);

                await _aiLearningDataRepository.UpdateAsync(learningData);
            }
        }
        catch
        {
            // Non-critical operation - don't fail the approval if learning data update fails
            // In production, log this error for monitoring
        }
    }

    /// <summary>
    /// Builds the response DTO
    /// </summary>
    private ApproveOrRejectDiscountRequestResponse BuildResponse(
        DiscountRequest discountRequest,
        Approval approval,
        User approver,
        Customer customer,
        int slaTimeInSeconds)
    {
        var response = new ApproveOrRejectDiscountRequestResponse
        {
            DiscountRequestId = discountRequest.Id,
            ApprovalId = approval.Id,
            Status = discountRequest.Status,
            Decision = approval.Decision,
            ApproverName = approver.Name,
            SlaTimeInSeconds = slaTimeInSeconds,
            SlaTimeFormatted = FormatSlaTime(slaTimeInSeconds),
            SlaMet = null, // TODO: Compare with company SLA thresholds
            Justification = approval.Justification,
            DecisionDateTime = approval.DecisionDateTime,
            CustomerName = customer.Name,
            RequestedDiscountPercentage = discountRequest.RequestedDiscountPercentage,
            EstimatedMarginPercentage = discountRequest.EstimatedMarginPercentage ?? 0m
        };

        // Build summary message
        response.Summary = approval.Decision switch
        {
            ApprovalDecision.Approve => $"Discount request approved by {approver.Name}. The salesperson can proceed with the negotiation.",
            ApprovalDecision.Reject => $"Discount request rejected by {approver.Name}. Reason: {approval.Justification}",
            ApprovalDecision.RequestAdjustment => $"Adjustment requested by {approver.Name}. The salesperson needs to revise the request.",
            _ => "Decision recorded"
        };

        // Build next steps
        response.NextSteps = BuildNextSteps(approval.Decision, approver.Name);

        return response;
    }

    /// <summary>
    /// Formats SLA time as human-readable string
    /// </summary>
    private string FormatSlaTime(int slaTimeInSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(slaTimeInSeconds);

        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        
        return $"{timeSpan.Seconds}s";
    }

    /// <summary>
    /// Builds next steps based on decision
    /// </summary>
    private List<string> BuildNextSteps(ApprovalDecision decision, string approverName)
    {
        return decision switch
        {
            ApprovalDecision.Approve => new List<string>
            {
                "Notify the salesperson of the approval",
                "Proceed with the negotiation",
                "Complete the sale in your CRM/ERP system",
                "Monitor margin realization"
            },
            ApprovalDecision.Reject => new List<string>
            {
                "Notify the salesperson of the rejection",
                "Review the rejection reason with the sales team",
                "Consider alternative approaches or products",
                "Create a new request if conditions change"
            },
            ApprovalDecision.RequestAdjustment => new List<string>
            {
                "Notify the salesperson of the adjustment request",
                $"Review feedback from {approverName}",
                "Revise the discount request with suggested changes",
                "Resubmit for approval"
            },
            _ => new List<string> { "Review the decision details" }
        };
    }

    /// <summary>
    /// Internal class to hold validation context
    /// </summary>
    private class ValidationContext
    {
        public DiscountRequest DiscountRequest { get; set; } = null!;
        public User Approver { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
    }
}
