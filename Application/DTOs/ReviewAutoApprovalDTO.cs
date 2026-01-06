using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request to review an AI auto-approval decision
/// Allows human review and potential override of AI decisions
/// </summary>
public class ReviewAutoApprovalRequest
{
    /// <summary>
    /// ID of the discount request that was auto-approved
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// ID of the company (for multi-tenant validation)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// ID of the reviewer (Manager or Admin)
    /// </summary>
    public Guid ReviewerId { get; set; }

    /// <summary>
    /// Review decision: Confirm, Override (reject or adjust)
    /// </summary>
    public ReviewDecision Decision { get; set; }

    /// <summary>
    /// Comments or justification for the review
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// If overriding, the new decision
    /// </summary>
    public ApprovalDecision? OverrideDecision { get; set; }

    /// <summary>
    /// Justification if overriding the AI decision
    /// </summary>
    public string? OverrideJustification { get; set; }
}

/// <summary>
/// Review decision types
/// </summary>
public enum ReviewDecision
{
    /// <summary>
    /// Confirm AI's decision - no changes
    /// </summary>
    Confirm = 1,

    /// <summary>
    /// Override AI's decision with human judgment
    /// </summary>
    Override = 2
}

/// <summary>
/// Response after reviewing an auto-approval
/// </summary>
public class ReviewAutoApprovalResponse
{
    /// <summary>
    /// ID of the discount request
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// Original AI approval ID
    /// </summary>
    public Guid OriginalApprovalId { get; set; }

    /// <summary>
    /// New approval ID (if overridden)
    /// </summary>
    public Guid? ReviewApprovalId { get; set; }

    /// <summary>
    /// Review decision made
    /// </summary>
    public ReviewDecision ReviewDecision { get; set; }

    /// <summary>
    /// Current status of the discount request
    /// </summary>
    public DiscountRequestStatus Status { get; set; }

    /// <summary>
    /// Whether the AI decision was overridden
    /// </summary>
    public bool WasOverridden { get; set; }

    /// <summary>
    /// Reviewer name
    /// </summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>
    /// Review timestamp
    /// </summary>
    public DateTime ReviewedAt { get; set; }

    /// <summary>
    /// Original AI approval details
    /// </summary>
    public AIApprovalDetails OriginalAIApproval { get; set; } = new();

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Next steps
    /// </summary>
    public List<string> NextSteps { get; set; } = new();
}

/// <summary>
/// Details of the original AI approval
/// </summary>
public class AIApprovalDetails
{
    /// <summary>
    /// When AI approved
    /// </summary>
    public DateTime ApprovedAt { get; set; }

    /// <summary>
    /// Risk score at approval time
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// AI confidence
    /// </summary>
    public decimal? AIConfidence { get; set; }

    /// <summary>
    /// AI's justification
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Time since auto-approval (hours)
    /// </summary>
    public double HoursSinceApproval { get; set; }
}
