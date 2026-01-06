namespace Application.DTOs;

/// <summary>
/// Request DTO for generating AI decision explanation (IA-UC-03)
/// </summary>
public class ExplainDecisionRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Discount request ID (if available)
    /// </summary>
    public Guid? DiscountRequestId { get; set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Salesperson ID
    /// </summary>
    public Guid? SalespersonId { get; set; }

    /// <summary>
    /// Recommended discount percentage (from AI)
    /// </summary>
    public decimal? RecommendedDiscount { get; set; }

    /// <summary>
    /// Risk score calculated
    /// </summary>
    public decimal? RiskScore { get; set; }

    /// <summary>
    /// Was the request auto-approved by AI?
    /// </summary>
    public bool? WasAutoApproved { get; set; }

    /// <summary>
    /// Approval decision (if made)
    /// </summary>
    public string? Decision { get; set; }

    /// <summary>
    /// Type of explanation requested
    /// </summary>
    public ExplanationType Type { get; set; } = ExplanationType.General;

    /// <summary>
    /// Include detailed breakdown
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Use AI for explanation generation (if false, uses template-based)
    /// </summary>
    public bool UseAI { get; set; } = true;
}

/// <summary>
/// Type of explanation
/// </summary>
public enum ExplanationType
{
    /// <summary>
    /// General explanation of the decision
    /// </summary>
    General,

    /// <summary>
    /// Explanation focused on discount recommendation
    /// </summary>
    DiscountRecommendation,

    /// <summary>
    /// Explanation focused on risk score
    /// </summary>
    RiskScore,

    /// <summary>
    /// Explanation for auto-approval decision
    /// </summary>
    AutoApproval,

    /// <summary>
    /// Explanation for rejection
    /// </summary>
    Rejection
}

/// <summary>
/// Response DTO for AI decision explanation (IA-UC-03)
/// </summary>
public class ExplainDecisionResponse
{
    /// <summary>
    /// Simple, human-readable summary explanation
    /// Examples:
    /// - "Discount is common for this customer"
    /// - "Margin below historical standard"
    /// - "Customer has excellent payment history"
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed explanation points
    /// </summary>
    public List<string> Details { get; set; } = new();

    /// <summary>
    /// Key factors that influenced the decision
    /// </summary>
    public List<ExplanationFactor> KeyFactors { get; set; } = new();

    /// <summary>
    /// Positive indicators (factors that support the decision)
    /// </summary>
    public List<string> PositiveIndicators { get; set; } = new();

    /// <summary>
    /// Concerns or risk indicators
    /// </summary>
    public List<string> Concerns { get; set; } = new();

    /// <summary>
    /// Recommendations or next steps
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Confidence in the explanation (0-1)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Source of explanation
    /// </summary>
    public string Source { get; set; } = string.Empty; // "AI", "RuleBased", "Template"

    /// <summary>
    /// Indicates if this is a fallback explanation
    /// </summary>
    public bool IsFallback { get; set; }

    /// <summary>
    /// Type of explanation provided
    /// </summary>
    public ExplanationType Type { get; set; }

    /// <summary>
    /// Timestamp when explanation was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Explanation factor with impact level
/// </summary>
public class ExplanationFactor
{
    /// <summary>
    /// Factor name/category
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the factor
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Impact level: Positive, Negative, Neutral
    /// </summary>
    public string Impact { get; set; } = string.Empty;

    /// <summary>
    /// Importance weight (0-1)
    /// </summary>
    public decimal Weight { get; set; }
}
