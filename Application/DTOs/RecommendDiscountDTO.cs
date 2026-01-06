namespace Application.DTOs;

/// <summary>
/// Request DTO for recommending a discount (IA-UC-01)
/// </summary>
public class RecommendDiscountRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Salesperson ID
    /// </summary>
    public Guid SalespersonId { get; set; }

    /// <summary>
    /// Products in the discount request
    /// </summary>
    public List<ProductItemDTO> Products { get; set; } = new();

    /// <summary>
    /// Include customer history in analysis
    /// </summary>
    public bool IncludeCustomerHistory { get; set; } = true;

    /// <summary>
    /// Include business rules in analysis
    /// </summary>
    public bool IncludeBusinessRules { get; set; } = true;

    /// <summary>
    /// Use AI recommendation (if false, uses only rule-based fallback)
    /// </summary>
    public bool UseAI { get; set; } = true;
}

/// <summary>
/// Product item in a discount recommendation request
/// </summary>
public class ProductItemDTO
{
    /// <summary>
    /// Product ID
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Current discount being considered (optional)
    /// </summary>
    public decimal? CurrentDiscountPercentage { get; set; }
}

/// <summary>
/// Response DTO for discount recommendation (IA-UC-01)
/// </summary>
public class RecommendDiscountResponse
{
    /// <summary>
    /// Recommended discount percentage
    /// </summary>
    public decimal RecommendedDiscountPercentage { get; set; }

    /// <summary>
    /// Expected margin percentage after discount
    /// </summary>
    public decimal ExpectedMarginPercentage { get; set; }

    /// <summary>
    /// AI confidence level (0-1)
    /// 0 = no confidence, 1 = high confidence
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Brief explanation of the recommendation
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if fallback to rules was used (AI unavailable)
    /// </summary>
    public bool IsFallback { get; set; }

    /// <summary>
    /// Source of recommendation
    /// </summary>
    public string Source { get; set; } = string.Empty; // "AI", "RuleBased", "Hybrid"

    /// <summary>
    /// Additional recommendations or warnings
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Risk indicators
    /// </summary>
    public List<string> RiskIndicators { get; set; } = new();

    /// <summary>
    /// Minimum acceptable discount based on rules
    /// </summary>
    public decimal? MinimumDiscount { get; set; }

    /// <summary>
    /// Maximum allowed discount based on rules
    /// </summary>
    public decimal? MaximumDiscount { get; set; }

    /// <summary>
    /// Historical average discount for this customer
    /// </summary>
    public decimal? HistoricalAverageDiscount { get; set; }

    /// <summary>
    /// Timestamp of recommendation
    /// </summary>
    public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Indicates if recommendation is within company guardrails
    /// </summary>
    public bool IsWithinGuardrails { get; set; }
}
