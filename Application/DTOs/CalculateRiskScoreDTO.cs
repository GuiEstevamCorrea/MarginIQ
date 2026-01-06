namespace Application.DTOs;

/// <summary>
/// Request DTO for calculating risk score (IA-UC-02)
/// </summary>
public class CalculateRiskScoreRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Discount request ID (if already created)
    /// </summary>
    public Guid? DiscountRequestId { get; set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Salesperson ID
    /// </summary>
    public Guid SalespersonId { get; set; }

    /// <summary>
    /// Requested discount percentage
    /// </summary>
    public decimal RequestedDiscountPercentage { get; set; }

    /// <summary>
    /// Estimated margin percentage after discount
    /// </summary>
    public decimal? EstimatedMarginPercentage { get; set; }

    /// <summary>
    /// Products in the discount request
    /// </summary>
    public List<ProductItemDTO> Products { get; set; } = new();

    /// <summary>
    /// Use AI for risk calculation (if false, uses only rule-based)
    /// </summary>
    public bool UseAI { get; set; } = true;

    /// <summary>
    /// Include customer history in analysis
    /// </summary>
    public bool IncludeCustomerHistory { get; set; } = true;

    /// <summary>
    /// Include salesperson history in analysis
    /// </summary>
    public bool IncludeSalespersonHistory { get; set; } = true;
}

/// <summary>
/// Response DTO for risk score calculation (IA-UC-02)
/// </summary>
public class CalculateRiskScoreResponse
{
    /// <summary>
    /// Calculated risk score (0-100)
    /// 0 = Very low risk
    /// 100 = Maximum risk
    /// </summary>
    public decimal RiskScore { get; set; }

    /// <summary>
    /// Risk level classification
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty; // VeryLow, Low, Medium, High

    /// <summary>
    /// Key risk factors contributing to the score
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();

    /// <summary>
    /// AI confidence in the risk assessment (0-1)
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Indicates if fallback to rules was used (AI unavailable)
    /// </summary>
    public bool IsFallback { get; set; }

    /// <summary>
    /// Source of calculation
    /// </summary>
    public string Source { get; set; } = string.Empty; // "AI", "RuleBased", "Hybrid"

    /// <summary>
    /// Detailed breakdown of risk components
    /// </summary>
    public RiskBreakdown? Breakdown { get; set; }

    /// <summary>
    /// Indicates if human approval is required based on this risk score
    /// </summary>
    public bool RequiresHumanApproval { get; set; }

    /// <summary>
    /// Recommendations to mitigate risk
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Timestamp of calculation
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// Detailed breakdown of risk score components
/// </summary>
public class RiskBreakdown
{
    /// <summary>
    /// Customer history risk component (0-100)
    /// </summary>
    public decimal CustomerHistoryRisk { get; set; }

    /// <summary>
    /// Discount deviation risk component (0-100)
    /// </summary>
    public decimal DiscountDeviationRisk { get; set; }

    /// <summary>
    /// Salesperson behavior risk component (0-100)
    /// </summary>
    public decimal SalespersonBehaviorRisk { get; set; }

    /// <summary>
    /// Margin impact risk component (0-100)
    /// </summary>
    public decimal MarginImpactRisk { get; set; }

    /// <summary>
    /// Weight factors used in calculation
    /// </summary>
    public Dictionary<string, decimal> Weights { get; set; } = new();
}
