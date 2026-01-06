using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request to approve or reject a discount request
/// </summary>
public class ApproveOrRejectDiscountRequestRequest
{
    /// <summary>
    /// ID of the discount request to approve/reject
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// ID of the manager/user making the decision
    /// </summary>
    public Guid ApproverId { get; set; }

    /// <summary>
    /// ID of the company (for multi-tenant validation)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Approval decision: Approve, Reject, RequestAdjustment
    /// </summary>
    public ApprovalDecision Decision { get; set; }

    /// <summary>
    /// Justification or comments for the decision
    /// Mandatory when rejecting
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Additional metadata (optional)
    /// Can include reasons, risk factors considered, etc.
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Response after approving or rejecting a discount request
/// </summary>
public class ApproveOrRejectDiscountRequestResponse
{
    /// <summary>
    /// ID of the discount request
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// ID of the approval record created
    /// </summary>
    public Guid ApprovalId { get; set; }

    /// <summary>
    /// New status of the discount request
    /// </summary>
    public DiscountRequestStatus Status { get; set; }

    /// <summary>
    /// Decision made
    /// </summary>
    public ApprovalDecision Decision { get; set; }

    /// <summary>
    /// Name of the approver
    /// </summary>
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>
    /// SLA time in seconds - time taken from request creation to decision
    /// </summary>
    public int SlaTimeInSeconds { get; set; }

    /// <summary>
    /// SLA time formatted as human-readable string
    /// </summary>
    public string SlaTimeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Whether the SLA was met (if SLA thresholds are configured)
    /// </summary>
    public bool? SlaMet { get; set; }

    /// <summary>
    /// Justification provided by the approver
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Date and time of the decision
    /// </summary>
    public DateTime DecisionDateTime { get; set; }

    /// <summary>
    /// Summary message for the user
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Next steps or actions for the salesperson
    /// </summary>
    public List<string> NextSteps { get; set; } = new();

    /// <summary>
    /// Customer name (for context)
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Requested discount percentage
    /// </summary>
    public decimal RequestedDiscountPercentage { get; set; }

    /// <summary>
    /// Estimated margin percentage
    /// </summary>
    public decimal EstimatedMarginPercentage { get; set; }
}
