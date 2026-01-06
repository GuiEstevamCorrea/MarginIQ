using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case UC-03B: Review Auto-Approval
/// 
/// Allows managers to review and potentially override AI auto-approval decisions.
/// This provides human oversight and governance over AI decisions.
/// 
/// Business rules:
/// - Only Manager or Admin can review auto-approvals
/// - Can only review requests with AutoApproved status
/// - Review is recorded in audit log for transparency
/// - Override creates a new human approval record
/// - Learning data is updated with human feedback
/// </summary>
public class ReviewAutoApprovalUseCase
{
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAILearningDataRepository _aiLearningDataRepository;

    public ReviewAutoApprovalUseCase(
        IDiscountRequestRepository discountRequestRepository,
        IApprovalRepository approvalRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        IAILearningDataRepository aiLearningDataRepository)
    {
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _approvalRepository = approvalRepository ?? throw new ArgumentNullException(nameof(approvalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _aiLearningDataRepository = aiLearningDataRepository ?? throw new ArgumentNullException(nameof(aiLearningDataRepository));
    }

    /// <summary>
    /// Executes the review auto-approval use case
    /// </summary>
    public async Task<ReviewAutoApprovalResponse> ExecuteAsync(
        ReviewAutoApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Step 1: Validate and load context
        var context = await ValidateAndLoadContextAsync(request, cancellationToken);

        // Step 2: Validate business rules
        ValidateBusinessRules(request, context);

        // Step 3: Process review decision
        Approval? overrideApproval = null;
        if (request.Decision == ReviewDecision.Override)
        {
            overrideApproval = await ProcessOverrideAsync(request, context, cancellationToken);
        }
        else
        {
            await ProcessConfirmationAsync(context, cancellationToken);
        }

        // Step 4: Create audit log
        await CreateAuditLogAsync(request, context, overrideApproval, cancellationToken);

        // Step 5: Update learning data with human feedback
        await UpdateLearningDataAsync(request, context, cancellationToken);

        // Step 6: Build response
        return BuildResponse(request, context, overrideApproval);
    }

    /// <summary>
    /// Validates request and loads context
    /// </summary>
    private async Task<ReviewContext> ValidateAndLoadContextAsync(
        ReviewAutoApprovalRequest request,
        CancellationToken cancellationToken)
    {
        // Load discount request
        var discountRequest = await _discountRequestRepository.GetByIdAsync(request.DiscountRequestId, cancellationToken);
        if (discountRequest == null)
            throw new InvalidOperationException($"Discount request with ID {request.DiscountRequestId} not found");

        // Validate multi-tenant
        if (discountRequest.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Discount request does not belong to the specified company");

        // Load reviewer
        var reviewer = await _userRepository.GetByIdAsync(request.ReviewerId, cancellationToken);
        if (reviewer == null)
            throw new InvalidOperationException($"Reviewer with ID {request.ReviewerId} not found");

        if (reviewer.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Reviewer does not belong to the specified company");

        // Validate reviewer has permission
        if (reviewer.Role != UserRole.Manager && reviewer.Role != UserRole.Admin)
            throw new InvalidOperationException($"User does not have permission to review auto-approvals. Required role: Manager or Admin");

        // Load original AI approval
        var approvals = await _approvalRepository.GetByDiscountRequestIdAsync(request.DiscountRequestId, cancellationToken);
        var aiApproval = approvals
            .Where(a => a.Source == ApprovalSource.AI && a.Decision == ApprovalDecision.Approve)
            .OrderByDescending(a => a.DecisionDateTime)
            .FirstOrDefault();

        if (aiApproval == null)
            throw new InvalidOperationException("No AI approval found for this discount request");

        return new ReviewContext
        {
            DiscountRequest = discountRequest,
            Reviewer = reviewer,
            OriginalAIApproval = aiApproval
        };
    }

    /// <summary>
    /// Validates business rules
    /// </summary>
    private void ValidateBusinessRules(ReviewAutoApprovalRequest request, ReviewContext context)
    {
        // Can only review auto-approved requests
        if (context.DiscountRequest.Status != DiscountRequestStatus.AutoApprovedByAI)
        {
            throw new InvalidOperationException(
                $"Cannot review request with status {context.DiscountRequest.Status}. " +
                $"Only auto-approved requests can be reviewed.");
        }

        // If overriding, must provide override decision and justification
        if (request.Decision == ReviewDecision.Override)
        {
            if (!request.OverrideDecision.HasValue)
                throw new InvalidOperationException("Override decision is required when overriding AI approval");

            if (string.IsNullOrWhiteSpace(request.OverrideJustification))
                throw new InvalidOperationException("Override justification is required when overriding AI approval");

            // Cannot override with Approve (it's already approved)
            if (request.OverrideDecision.Value == ApprovalDecision.Approve)
                throw new InvalidOperationException("Cannot override an approval with another approval. Use Confirm instead.");
        }
    }

    /// <summary>
    /// Processes override decision
    /// </summary>
    private async Task<Approval> ProcessOverrideAsync(
        ReviewAutoApprovalRequest request,
        ReviewContext context,
        CancellationToken cancellationToken)
    {
        // Update discount request status based on override decision
        switch (request.OverrideDecision!.Value)
        {
            case ApprovalDecision.Reject:
                context.DiscountRequest.Reject();
                break;

            case ApprovalDecision.RequestAdjustment:
                context.DiscountRequest.RequestAdjustment();
                break;

            default:
                throw new InvalidOperationException($"Invalid override decision: {request.OverrideDecision}");
        }

        await _discountRequestRepository.UpdateAsync(context.DiscountRequest, cancellationToken);

        // Calculate SLA from original request creation
        var slaTimeInSeconds = (int)(DateTime.UtcNow - context.DiscountRequest.CreatedAt).TotalSeconds;

        // Create human override approval
        var overrideApproval = new Approval(
            discountRequestId: context.DiscountRequest.Id,
            approverId: context.Reviewer.Id,
            decision: request.OverrideDecision.Value,
            slaTimeInSeconds: slaTimeInSeconds,
            justification: $"Override of AI auto-approval. {request.OverrideJustification}",
            metadata: System.Text.Json.JsonSerializer.Serialize(new
            {
                overriddenApprovalId = context.OriginalAIApproval.Id,
                originalDecision = "AutoApprove",
                reviewComments = request.Comments
            }));

        await _approvalRepository.AddAsync(overrideApproval, cancellationToken);

        return overrideApproval;
    }

    /// <summary>
    /// Processes confirmation (no changes)
    /// </summary>
    private async Task ProcessConfirmationAsync(
        ReviewContext context,
        CancellationToken cancellationToken)
    {
        // No status change needed - just record the review
        // The audit log will capture that a human reviewed and confirmed the AI decision
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates audit log
    /// </summary>
    private async Task CreateAuditLogAsync(
        ReviewAutoApprovalRequest request,
        ReviewContext context,
        Approval? overrideApproval,
        CancellationToken cancellationToken)
    {
        var action = request.Decision == ReviewDecision.Override
            ? AuditAction.StatusChanged
            : AuditAction.Other;

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            discountRequestId = context.DiscountRequest.Id,
            originalAIApprovalId = context.OriginalAIApproval.Id,
            reviewerId = context.Reviewer.Id,
            reviewerName = context.Reviewer.Name,
            reviewDecision = request.Decision.ToString(),
            wasOverridden = request.Decision == ReviewDecision.Override,
            overrideDecision = request.OverrideDecision?.ToString(),
            overrideJustification = request.OverrideJustification,
            comments = request.Comments,
            newStatus = context.DiscountRequest.Status.ToString(),
            overrideApprovalId = overrideApproval?.Id
        });

        var auditLog = AuditLog.CreateForHuman(
            entityName: nameof(DiscountRequest),
            entityId: context.DiscountRequest.Id,
            action: action,
            companyId: context.DiscountRequest.CompanyId,
            userId: context.Reviewer.Id,
            payload: payload);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Updates learning data with human feedback
    /// Important for AI to learn from human corrections
    /// </summary>
    private async Task UpdateLearningDataAsync(
        ReviewAutoApprovalRequest request,
        ReviewContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var learningData = await _aiLearningDataRepository.GetByDiscountRequestIdAsync(
                context.DiscountRequest.Id,
                cancellationToken);

            if (learningData != null)
            {
                // If override, update with corrected decision
                if (request.Decision == ReviewDecision.Override && request.OverrideDecision.HasValue)
                {
                    var approved = request.OverrideDecision.Value == ApprovalDecision.Approve;
                    var approvedDiscount = approved ? context.DiscountRequest.RequestedDiscountPercentage : (decimal?)null;
                    
                    learningData.RecordHumanDecision(approved, approvedDiscount);
                    await _aiLearningDataRepository.UpdateAsync(learningData);
                }
                // If confirm, the AI decision stands - no update needed
            }
        }
        catch
        {
            // Non-critical
        }
    }

    /// <summary>
    /// Builds response
    /// </summary>
    private ReviewAutoApprovalResponse BuildResponse(
        ReviewAutoApprovalRequest request,
        ReviewContext context,
        Approval? overrideApproval)
    {
        var hoursSinceApproval = (DateTime.UtcNow - context.OriginalAIApproval.DecisionDateTime).TotalHours;

        var response = new ReviewAutoApprovalResponse
        {
            DiscountRequestId = context.DiscountRequest.Id,
            OriginalApprovalId = context.OriginalAIApproval.Id,
            ReviewApprovalId = overrideApproval?.Id,
            ReviewDecision = request.Decision,
            Status = context.DiscountRequest.Status,
            WasOverridden = request.Decision == ReviewDecision.Override,
            ReviewerName = context.Reviewer.Name,
            ReviewedAt = DateTime.UtcNow,
            OriginalAIApproval = new AIApprovalDetails
            {
                ApprovedAt = context.OriginalAIApproval.DecisionDateTime,
                RiskScore = context.DiscountRequest.RiskScore ?? 0m,
                AIConfidence = null, // Could parse from metadata
                Justification = context.OriginalAIApproval.Justification,
                HoursSinceApproval = hoursSinceApproval
            }
        };

        if (request.Decision == ReviewDecision.Confirm)
        {
            response.Summary = $"✅ AI auto-approval confirmed by {context.Reviewer.Name}. The AI's decision was correct.";
            response.NextSteps = new List<string>
            {
                "The auto-approval stands",
                "Proceed with the negotiation",
                "Complete the sale in your CRM/ERP system",
                "This confirmation helps the AI learn and improve"
            };
        }
        else
        {
            var overrideAction = request.OverrideDecision == ApprovalDecision.Reject ? "rejected" : "sent back for adjustment";
            response.Summary = $"⚠️ AI auto-approval overridden by {context.Reviewer.Name}. Request {overrideAction}.";
            response.NextSteps = new List<string>
            {
                "Notify the salesperson of the override",
                $"Reason: {request.OverrideJustification}",
                "The AI will learn from this correction",
                request.OverrideDecision == ApprovalDecision.Reject
                    ? "Consider alternative approaches with the customer"
                    : "Salesperson should revise and resubmit the request"
            };
        }

        return response;
    }

    /// <summary>
    /// Internal review context
    /// </summary>
    private class ReviewContext
    {
        public DiscountRequest DiscountRequest { get; set; } = null!;
        public User Reviewer { get; set; } = null!;
        public Approval OriginalAIApproval { get; set; } = null!;
    }
}
