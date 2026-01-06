using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a learning data point for AI training.
/// Stores historical data about discount requests, decisions, and outcomes.
/// Data stored:
/// - Customer
/// - Product
/// - Discount
/// - Margin
/// - Decision
/// - Sale outcome (won/lost)
/// </summary>
public class AILearningData
{
    /// <summary>
    /// Unique identifier for the learning data point
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The company (tenant) this learning data belongs to
    /// AI learns per company (logically separated model)
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Reference to the discount request
    /// </summary>
    public Guid DiscountRequestId { get; private set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Customer name (denormalized for analysis)
    /// </summary>
    public string CustomerName { get; private set; }

    /// <summary>
    /// Customer segment
    /// </summary>
    public string? CustomerSegment { get; private set; }

    /// <summary>
    /// Customer classification (A/B/C)
    /// </summary>
    public CustomerClassification CustomerClassification { get; private set; }

    /// <summary>
    /// Salesperson ID
    /// </summary>
    public Guid SalespersonId { get; private set; }

    /// <summary>
    /// Salesperson name (denormalized)
    /// </summary>
    public string SalespersonName { get; private set; }

    /// <summary>
    /// Salesperson role at the time of request
    /// </summary>
    public UserRole SalespersonRole { get; private set; }

    /// <summary>
    /// Product information (JSON array with product IDs, names, categories, quantities)
    /// Stored as JSON to support multiple products in one request
    /// </summary>
    public string ProductsJson { get; private set; }

    /// <summary>
    /// Requested discount percentage
    /// </summary>
    public decimal RequestedDiscountPercentage { get; private set; }

    /// <summary>
    /// Final approved discount percentage (may differ from requested)
    /// </summary>
    public decimal? ApprovedDiscountPercentage { get; private set; }

    /// <summary>
    /// Estimated margin percentage before discount
    /// </summary>
    public decimal BaseMarginPercentage { get; private set; }

    /// <summary>
    /// Final margin percentage after discount
    /// </summary>
    public decimal FinalMarginPercentage { get; private set; }

    /// <summary>
    /// Total base price (before discount)
    /// </summary>
    public decimal TotalBasePrice { get; private set; }

    /// <summary>
    /// Total final price (after discount)
    /// </summary>
    public decimal TotalFinalPrice { get; private set; }

    /// <summary>
    /// Currency used
    /// </summary>
    public string Currency { get; private set; }

    /// <summary>
    /// Decision made (Approved, Rejected, AdjustmentRequested)
    /// </summary>
    public ApprovalDecision Decision { get; private set; }

    /// <summary>
    /// Source of decision (Human or AI)
    /// </summary>
    public ApprovalSource DecisionSource { get; private set; }

    /// <summary>
    /// Risk score calculated at the time (0-100)
    /// </summary>
    public decimal RiskScore { get; private set; }

    /// <summary>
    /// AI confidence level when recommendation was made (0-1)
    /// Null if AI was not used
    /// </summary>
    public decimal? AIConfidence { get; private set; }

    /// <summary>
    /// Sale outcome: Won (true), Lost (false), Unknown (null)
    /// This is critical for learning - updated after the sale process
    /// </summary>
    public bool? SaleOutcome { get; private set; }

    /// <summary>
    /// Date and time when the sale outcome was determined
    /// </summary>
    public DateTime? SaleOutcomeDate { get; private set; }

    /// <summary>
    /// Reason for sale outcome (optional)
    /// </summary>
    public string? SaleOutcomeReason { get; private set; }

    /// <summary>
    /// Time taken to make the decision (in seconds)
    /// Important for measuring SLA and process efficiency
    /// </summary>
    public int DecisionTimeSec { get; private set; }

    /// <summary>
    /// Additional context stored as JSON (customer history, market conditions, etc.)
    /// </summary>
    public string? ContextJson { get; private set; }

    /// <summary>
    /// Date and time when the discount request was created
    /// </summary>
    public DateTime RequestCreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the decision was made
    /// </summary>
    public DateTime DecisionMadeAt { get; private set; }

    /// <summary>
    /// Date and time when this learning data was recorded
    /// </summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>
    /// Indicates if this data has been used for training
    /// </summary>
    public bool UsedForTraining { get; private set; }

    /// <summary>
    /// Date and time when this data was used for training
    /// </summary>
    public DateTime? TrainedAt { get; private set; }

    // Navigation properties
    public Company? Company { get; private set; }
    public DiscountRequest? DiscountRequest { get; private set; }

    // Private constructor for EF Core
    private AILearningData() 
    {
        CustomerName = string.Empty;
        SalespersonName = string.Empty;
        ProductsJson = string.Empty;
        Currency = string.Empty;
    }

    /// <summary>
    /// Creates a new AI learning data point from a discount request and its approval
    /// </summary>
    public AILearningData(
        Guid companyId,
        Guid discountRequestId,
        Guid customerId,
        string customerName,
        string? customerSegment,
        CustomerClassification customerClassification,
        Guid salespersonId,
        string salespersonName,
        UserRole salespersonRole,
        string productsJson,
        decimal requestedDiscountPercentage,
        decimal? approvedDiscountPercentage,
        decimal baseMarginPercentage,
        decimal finalMarginPercentage,
        decimal totalBasePrice,
        decimal totalFinalPrice,
        string currency,
        ApprovalDecision decision,
        ApprovalSource decisionSource,
        decimal riskScore,
        decimal? aiConfidence,
        DateTime requestCreatedAt,
        DateTime decisionMadeAt,
        string? contextJson = null)
    {
        ValidateCustomerName(customerName);
        ValidateSalespersonName(salespersonName);
        ValidateProductsJson(productsJson);
        ValidateCurrency(currency);

        Id = Guid.NewGuid();
        CompanyId = companyId;
        DiscountRequestId = discountRequestId;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerSegment = customerSegment;
        CustomerClassification = customerClassification;
        SalespersonId = salespersonId;
        SalespersonName = salespersonName;
        SalespersonRole = salespersonRole;
        ProductsJson = productsJson;
        RequestedDiscountPercentage = requestedDiscountPercentage;
        ApprovedDiscountPercentage = approvedDiscountPercentage;
        BaseMarginPercentage = baseMarginPercentage;
        FinalMarginPercentage = finalMarginPercentage;
        TotalBasePrice = totalBasePrice;
        TotalFinalPrice = totalFinalPrice;
        Currency = currency;
        Decision = decision;
        DecisionSource = decisionSource;
        RiskScore = riskScore;
        AIConfidence = aiConfidence;
        RequestCreatedAt = requestCreatedAt;
        DecisionMadeAt = decisionMadeAt;
        DecisionTimeSec = (int)(decisionMadeAt - requestCreatedAt).TotalSeconds;
        ContextJson = contextJson;
        RecordedAt = DateTime.UtcNow;
        UsedForTraining = false;
    }

    /// <summary>
    /// Updates the sale outcome (won/lost)
    /// Critical for AI learning - should be updated after the sale process completes
    /// </summary>
    /// <param name="won">True if sale was won, false if lost</param>
    /// <param name="reason">Optional reason for the outcome</param>
    public void UpdateSaleOutcome(bool won, string? reason = null)
    {
        SaleOutcome = won;
        SaleOutcomeDate = DateTime.UtcNow;
        SaleOutcomeReason = reason;
    }

    /// <summary>
    /// Records the human decision for this learning data
    /// Updates the approved discount percentage and decision details
    /// </summary>
    /// <param name="approved">Whether the request was approved</param>
    /// <param name="approvedDiscountPercentage">The approved discount percentage (if approved)</param>
    public void RecordHumanDecision(bool approved, decimal? approvedDiscountPercentage = null)
    {
        Decision = approved ? ApprovalDecision.Approve : ApprovalDecision.Reject;
        DecisionSource = ApprovalSource.Human;
        ApprovedDiscountPercentage = approvedDiscountPercentage;
        DecisionMadeAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this data as used for training
    /// </summary>
    public void MarkAsUsedForTraining()
    {
        if (UsedForTraining)
            throw new InvalidOperationException("This learning data has already been marked as used for training");

        UsedForTraining = true;
        TrainedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this learning data is complete (has sale outcome)
    /// Complete data is more valuable for training
    /// </summary>
    public bool IsComplete() => SaleOutcome.HasValue;

    /// <summary>
    /// Checks if this learning data is ready for training
    /// Ready = Complete and not yet used for training
    /// </summary>
    public bool IsReadyForTraining() => IsComplete() && !UsedForTraining;

    /// <summary>
    /// Gets the decision accuracy indicator
    /// - If sale was won after approval: Good decision
    /// - If sale was lost after approval: Questionable decision
    /// - If sale was won after rejection: Missed opportunity
    /// </summary>
    public string? GetDecisionQuality()
    {
        if (!SaleOutcome.HasValue)
            return null;

        if (Decision == ApprovalDecision.Approve)
        {
            return SaleOutcome.Value ? "Good" : "Questionable";
        }
        else if (Decision == ApprovalDecision.Reject)
        {
            return SaleOutcome.Value ? "MissedOpportunity" : "Good";
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets the age of this learning data in days
    /// </summary>
    public int GetAgeDays() => (DateTime.UtcNow - RecordedAt).Days;

    private static void ValidateCustomerName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty", nameof(name));
    }

    private static void ValidateSalespersonName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Salesperson name cannot be empty", nameof(name));
    }

    private static void ValidateProductsJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Products JSON cannot be empty", nameof(json));
    }

    private static void ValidateCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));
    }
}
