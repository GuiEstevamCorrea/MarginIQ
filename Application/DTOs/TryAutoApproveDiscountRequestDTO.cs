using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request to try auto-approval on an existing discount request
/// Can be used to retry auto-approval on requests that were sent for human review
/// </summary>
public class TryAutoApproveDiscountRequestRequest
{
    /// <summary>
    /// ID of the discount request to try auto-approval on
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// ID of the company (for multi-tenant validation)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// ID of the user requesting the auto-approval attempt (for audit)
    /// </summary>
    public Guid RequestedBy { get; set; }

    /// <summary>
    /// Force re-evaluation even if already evaluated
    /// </summary>
    public bool ForceReEvaluation { get; set; }
}

/// <summary>
/// Response after attempting auto-approval
/// </summary>
public class TryAutoApproveDiscountRequestResponse
{
    /// <summary>
    /// ID of the discount request
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// Whether auto-approval was successful
    /// </summary>
    public bool WasAutoApproved { get; set; }

    /// <summary>
    /// ID of the approval record (if approved)
    /// </summary>
    public Guid? ApprovalId { get; set; }

    /// <summary>
    /// New status of the discount request
    /// </summary>
    public DiscountRequestStatus Status { get; set; }

    /// <summary>
    /// Risk score calculated
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Risk level (Low, Medium, High, VeryHigh)
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// AI confidence level (if AI was used)
    /// </summary>
    public decimal? AIConfidence { get; set; }

    /// <summary>
    /// Reason why auto-approval succeeded or failed
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Detailed evaluation results
    /// </summary>
    public AutoApprovalEvaluationDetails EvaluationDetails { get; set; } = new();

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Next steps for the user
    /// </summary>
    public List<string> NextSteps { get; set; } = new();

    /// <summary>
    /// Timestamp of the evaluation
    /// </summary>
    public DateTime EvaluatedAt { get; set; }
}

/// <summary>
/// Detailed auto-approval evaluation results
/// </summary>
public class AutoApprovalEvaluationDetails
{
    /// <summary>
    /// Whether guardrails validation passed
    /// </summary>
    public bool GuardrailsPassed { get; set; }

    /// <summary>
    /// Guardrails validation messages
    /// </summary>
    public List<string> GuardrailsMessages { get; set; } = new();

    /// <summary>
    /// Whether risk score is within threshold
    /// </summary>
    public bool RiskScoreWithinThreshold { get; set; }

    /// <summary>
    /// Maximum allowed risk score
    /// </summary>
    public decimal MaxRiskScoreThreshold { get; set; }

    /// <summary>
    /// Whether AI confidence is sufficient
    /// </summary>
    public bool AIConfidenceSufficient { get; set; }

    /// <summary>
    /// Minimum required AI confidence
    /// </summary>
    public decimal MinAIConfidenceThreshold { get; set; }

    /// <summary>
    /// Whether AI is enabled for this company
    /// </summary>
    public bool AIEnabled { get; set; }

    /// <summary>
    /// Blocking reasons (if auto-approval failed)
    /// </summary>
    public List<string> BlockingReasons { get; set; } = new();
}
