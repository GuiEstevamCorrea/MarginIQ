namespace Application.DTOs;

/// <summary>
/// Request DTO for creating a discount request (UC-01)
/// </summary>
public class CreateDiscountRequestRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Customer ID for whom the discount is requested
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Salesperson (User) ID creating the request
    /// </summary>
    public Guid SalespersonId { get; set; }

    /// <summary>
    /// Items in the discount request (products, quantities, prices)
    /// </summary>
    public List<DiscountRequestItemDTO> Items { get; set; } = new();

    /// <summary>
    /// Overall requested discount percentage (0-100)
    /// </summary>
    public decimal RequestedDiscountPercentage { get; set; }

    /// <summary>
    /// Optional comments or justification from salesperson
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Request AI recommendation before creating
    /// If true, AI will suggest a discount before finalizing
    /// </summary>
    public bool RequestAIRecommendation { get; set; } = true;

    /// <summary>
    /// Use AI for risk score calculation
    /// If false, uses rule-based risk calculation only
    /// </summary>
    public bool UseAIForRiskScore { get; set; } = true;

    /// <summary>
    /// Allow auto-approval by AI if conditions are met
    /// If false, always requires human approval
    /// </summary>
    public bool AllowAutoApproval { get; set; } = true;
}

/// <summary>
/// Item in a discount request
/// </summary>
public class DiscountRequestItemDTO
{
    /// <summary>
    /// Product ID
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity of the product
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price (before discount)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage for this specific item (optional)
    /// If not provided, uses the overall request discount
    /// </summary>
    public decimal? ItemDiscountPercentage { get; set; }
}

/// <summary>
/// Response DTO for creating a discount request (UC-01)
/// </summary>
public class CreateDiscountRequestResponse
{
    /// <summary>
    /// Created discount request ID
    /// </summary>
    public Guid DiscountRequestId { get; set; }

    /// <summary>
    /// Status of the discount request
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Was the request auto-approved by AI?
    /// </summary>
    public bool WasAutoApproved { get; set; }

    /// <summary>
    /// Risk score calculated (0-100)
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Risk level classification
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Estimated margin percentage after discount
    /// </summary>
    public decimal EstimatedMarginPercentage { get; set; }

    /// <summary>
    /// AI recommendation (if requested)
    /// </summary>
    public AIRecommendationSummary? AIRecommendation { get; set; }

    /// <summary>
    /// Business rules validation result
    /// </summary>
    public BusinessRulesValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Next steps or actions required
    /// </summary>
    public List<string> NextSteps { get; set; } = new();

    /// <summary>
    /// Warnings or important information
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Approval ID (if auto-approved)
    /// </summary>
    public Guid? ApprovalId { get; set; }

    /// <summary>
    /// Timestamp when request was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Summary message for display
    /// </summary>
    public string Summary => BuildSummary();

    private string BuildSummary()
    {
        if (WasAutoApproved)
        {
            return $"Discount request auto-approved by AI (Risk: {RiskScore:F0}/100, Margin: {EstimatedMarginPercentage:F1}%)";
        }

        return $"Discount request created - Pending approval (Risk: {RiskScore:F0}/100, Margin: {EstimatedMarginPercentage:F1}%)";
    }
}

/// <summary>
/// AI recommendation summary
/// </summary>
public class AIRecommendationSummary
{
    /// <summary>
    /// Recommended discount percentage
    /// </summary>
    public decimal RecommendedDiscount { get; set; }

    /// <summary>
    /// Expected margin with recommended discount
    /// </summary>
    public decimal ExpectedMargin { get; set; }

    /// <summary>
    /// AI confidence in recommendation (0-1)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Difference between requested and recommended discount
    /// </summary>
    public decimal DeviationFromRecommended => Math.Abs(RecommendedDiscount - 0); // Will be calculated in use case

    /// <summary>
    /// Source of recommendation (AI or RuleBased)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Brief explanation
    /// </summary>
    public string? Explanation { get; set; }
}

/// <summary>
/// Business rules validation result
/// </summary>
public class BusinessRulesValidationResult
{
    /// <summary>
    /// Is the discount request valid according to business rules?
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of violated rules
    /// </summary>
    public List<string> ViolatedRules { get; set; } = new();

    /// <summary>
    /// List of warnings (non-blocking issues)
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Can the request proceed despite violations?
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Reason for blocking (if not valid and cannot proceed)
    /// </summary>
    public string? BlockingReason { get; set; }

    /// <summary>
    /// Applied business rules
    /// </summary>
    public List<AppliedBusinessRule> AppliedRules { get; set; } = new();
}

/// <summary>
/// Applied business rule information
/// </summary>
public class AppliedBusinessRule
{
    public Guid RuleId { get; set; }
    public string RuleType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? Message { get; set; }
}
