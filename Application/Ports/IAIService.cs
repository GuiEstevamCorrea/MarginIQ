namespace Application.Ports;

/// <summary>
/// Port (interface) for AI/ML services.
/// AI does NOT live in the domain - it enters as an external adapter.
/// Architecture: Application → Port AI → Adapter AI
/// Fallback is always available to fixed rules.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// AI-UC-01: Recommends a discount percentage for a discount request
    /// </summary>
    /// <param name="request">Discount recommendation request context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Discount recommendation result</returns>
    Task<DiscountRecommendation> RecommendDiscountAsync(
        DiscountRecommendationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// AI-UC-02: Calculates risk score for a discount request
    /// Analyzes deviation from historical pattern
    /// Returns score 0-100
    /// </summary>
    /// <param name="request">Risk score calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risk score (0-100)</returns>
    Task<AIRiskScore> CalculateRiskScoreAsync(
        RiskScoreRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// AI-UC-03: Provides explainability for AI decisions
    /// Generates simple text explanations like:
    /// - "Discount is common for this customer"
    /// - "Margin below historical standard"
    /// </summary>
    /// <param name="request">Explainability request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Human-readable explanation</returns>
    Task<AIExplanation> ExplainDecisionAsync(
        ExplainabilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// AI-UC-04: Triggers incremental learning/training
    /// Periodic training based on:
    /// - Human decisions
    /// - Actual sale outcome (won/lost)
    /// </summary>
    /// <param name="request">Training request with feedback data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training result</returns>
    Task<TrainingResult> TrainModelAsync(
        ModelTrainingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if AI is enabled and healthy for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if AI is available and operational</returns>
    Task<bool> IsAvailableAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI governance settings for a company
    /// - Enable/disable AI per company
    /// - Adjust autonomy level
    /// - Complete audit of decisions
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI governance configuration</returns>
    Task<AIGovernanceSettings> GetGovernanceSettingsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates AI governance settings for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="settings">New governance settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateGovernanceSettingsAsync(
        Guid companyId,
        AIGovernanceSettings settings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for discount recommendation (AI-UC-01)
/// </summary>
public class DiscountRecommendationRequest
{
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SalespersonId { get; set; }
    public List<RecommendationItem> Items { get; set; } = new();
    
    /// <summary>
    /// Customer historical data
    /// </summary>
    public CustomerHistoryData? CustomerHistory { get; set; }
    
    /// <summary>
    /// Applicable business rules
    /// </summary>
    public List<BusinessRuleData> BusinessRules { get; set; } = new();
}

/// <summary>
/// Item in a discount recommendation request
/// </summary>
public class RecommendationItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCategory { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Customer historical data for AI analysis
/// </summary>
public class CustomerHistoryData
{
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal AverageDiscount { get; set; }
    public decimal MaxDiscountReceived { get; set; }
    public int RejectedRequests { get; set; }
    public string Classification { get; set; } = "Unclassified"; // A/B/C
    public bool HasPaymentIssues { get; set; }
}

/// <summary>
/// Business rule data for AI context
/// </summary>
public class BusinessRuleData
{
    public string RuleType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Parameters { get; set; } = "{}";
}

/// <summary>
/// Result of discount recommendation (AI-UC-01)
/// </summary>
public class DiscountRecommendation
{
    /// <summary>
    /// Recommended discount percentage
    /// </summary>
    public decimal RecommendedDiscountPercentage { get; set; }
    
    /// <summary>
    /// Expected margin after discount
    /// </summary>
    public decimal ExpectedMarginPercentage { get; set; }
    
    /// <summary>
    /// AI confidence level (0-1)
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
    /// Timestamp of recommendation
    /// </summary>
    public DateTime RecommendedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for risk score calculation (AI-UC-02)
/// </summary>
public class RiskScoreRequest
{
    public Guid CompanyId { get; set; }
    public Guid DiscountRequestId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SalespersonId { get; set; }
    public decimal RequestedDiscountPercentage { get; set; }
    public decimal EstimatedMarginPercentage { get; set; }
    public CustomerHistoryData? CustomerHistory { get; set; }
    public SalespersonHistoryData? SalespersonHistory { get; set; }
}

/// <summary>
/// Salesperson historical data for risk analysis
/// </summary>
public class SalespersonHistoryData
{
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public decimal AverageDiscount { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal WinRate { get; set; }
}

/// <summary>
/// Result of AI risk score calculation (AI-UC-02)
/// </summary>
public class AIRiskScore
{
    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public decimal Score { get; set; }
    
    /// <summary>
    /// Risk level classification
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty; // VeryLow, Low, Medium, High
    
    /// <summary>
    /// Key factors contributing to the risk score
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();
    
    /// <summary>
    /// AI confidence in the risk assessment
    /// </summary>
    public decimal Confidence { get; set; }
    
    /// <summary>
    /// Indicates if fallback to rules was used
    /// </summary>
    public bool IsFallback { get; set; }
    
    /// <summary>
    /// Timestamp of calculation
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for explainability (AI-UC-03)
/// </summary>
public class ExplainabilityRequest
{
    public Guid CompanyId { get; set; }
    public Guid DiscountRequestId { get; set; }
    public decimal RecommendedDiscount { get; set; }
    public decimal RiskScore { get; set; }
    public bool WasAutoApproved { get; set; }
}

/// <summary>
/// AI explanation result (AI-UC-03)
/// </summary>
public class AIExplanation
{
    /// <summary>
    /// Simple, human-readable explanation
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
    /// Recommendations or warnings
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
    
    /// <summary>
    /// Timestamp of explanation generation
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request for model training (AI-UC-04)
/// </summary>
public class ModelTrainingRequest
{
    public Guid CompanyId { get; set; }
    
    /// <summary>
    /// Training data: historical decisions and outcomes
    /// </summary>
    public List<TrainingDataPoint> TrainingData { get; set; } = new();
    
    /// <summary>
    /// Type of training: Incremental or Full
    /// </summary>
    public TrainingType Type { get; set; } = TrainingType.Incremental;
}

/// <summary>
/// Training data point for AI learning
/// </summary>
public class TrainingDataPoint
{
    public Guid DiscountRequestId { get; set; }
    public decimal RequestedDiscount { get; set; }
    public decimal FinalMargin { get; set; }
    public string Decision { get; set; } = string.Empty; // Approved, Rejected, etc.
    public string DecisionSource { get; set; } = string.Empty; // Human, AI
    public bool? SaleOutcome { get; set; } // Won/Lost (nullable if not yet known)
    public DateTime DecisionDate { get; set; }
}

/// <summary>
/// Type of model training
/// </summary>
public enum TrainingType
{
    /// <summary>
    /// Incremental learning with new data
    /// </summary>
    Incremental,
    
    /// <summary>
    /// Full model retraining
    /// </summary>
    Full
}

/// <summary>
/// Result of model training
/// </summary>
public class TrainingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DataPointsProcessed { get; set; }
    public DateTime TrainedAt { get; set; } = DateTime.UtcNow;
    public string? ModelVersion { get; set; }
}

/// <summary>
/// AI governance settings for a company (5.4)
/// </summary>
public class AIGovernanceSettings
{
    /// <summary>
    /// Enable/disable AI for this company
    /// </summary>
    public bool AIEnabled { get; set; } = true;
    
    /// <summary>
    /// Autonomy level (0-100)
    /// 0 = AI only recommends, never auto-approves
    /// 100 = Full autonomy within guardrails
    /// </summary>
    public int AutonomyLevel { get; set; } = 50;
    
    /// <summary>
    /// Maximum risk score threshold for auto-approval
    /// </summary>
    public decimal MaxRiskScoreForAutoApproval { get; set; } = 60m;
    
    /// <summary>
    /// Minimum AI confidence required for auto-approval
    /// </summary>
    public decimal MinConfidenceForAutoApproval { get; set; } = 0.75m;
    
    /// <summary>
    /// Require human review for all AI decisions
    /// </summary>
    public bool RequireHumanReview { get; set; } = false;
    
    /// <summary>
    /// Enable AI decision auditing
    /// </summary>
    public bool EnableAudit { get; set; } = true;
    
    /// <summary>
    /// Enable explainability for all AI decisions
    /// </summary>
    public bool EnableExplainability { get; set; } = true;
    
    /// <summary>
    /// Maximum discount AI can auto-approve (percentage)
    /// </summary>
    public decimal MaxAutoApprovalDiscount { get; set; } = 15m;
    
    /// <summary>
    /// Enable incremental learning
    /// </summary>
    public bool EnableIncrementalLearning { get; set; } = true;
    
    /// <summary>
    /// Frequency of model retraining (in days)
    /// </summary>
    public int RetrainingFrequencyDays { get; set; } = 30;
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
