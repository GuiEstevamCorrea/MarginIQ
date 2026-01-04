using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents a Discount Request - the central entity of the system.
/// Contains all information about a discount request including items, pricing, status, and AI risk assessment.
/// Business rule: Blocked customers cannot receive new discount requests.
/// </summary>
public class DiscountRequest
{
    private readonly List<DiscountRequestItem> _items = new();

    /// <summary>
    /// Unique identifier for the discount request
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Customer ID this discount request is for
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Salesperson (User) who created the request
    /// </summary>
    public Guid SalespersonId { get; private set; }

    /// <summary>
    /// Items included in this discount request (products, quantities, prices)
    /// </summary>
    public IReadOnlyCollection<DiscountRequestItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Overall requested discount percentage (0-100)
    /// Can differ from individual item discounts
    /// </summary>
    public decimal RequestedDiscountPercentage { get; private set; }

    /// <summary>
    /// Current status of the discount request
    /// </summary>
    public DiscountRequestStatus Status { get; private set; }

    /// <summary>
    /// Risk score calculated by AI (0-100)
    /// Higher score = higher risk = requires human approval
    /// </summary>
    public decimal? RiskScore { get; private set; }

    /// <summary>
    /// Estimated margin percentage after discount (0-100)
    /// </summary>
    public decimal? EstimatedMarginPercentage { get; private set; }

    /// <summary>
    /// The company (tenant) this discount request belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Comments or justification from the salesperson
    /// </summary>
    public string? Comments { get; private set; }

    /// <summary>
    /// Date and time when the request was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the request was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Date and time when the request was approved/rejected
    /// </summary>
    public DateTime? DecisionAt { get; private set; }

    // Navigation properties
    public Customer? Customer { get; private set; }
    public User? Salesperson { get; private set; }
    public Company? Company { get; private set; }

    // Private constructor for EF Core
    private DiscountRequest() { }

    /// <summary>
    /// Creates a new DiscountRequest instance
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <param name="salespersonId">Salesperson (User) ID</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="items">List of items in the request</param>
    /// <param name="requestedDiscountPercentage">Overall requested discount percentage</param>
    /// <param name="comments">Optional comments or justification</param>
    public DiscountRequest(
        Guid customerId,
        Guid salespersonId,
        Guid companyId,
        IEnumerable<DiscountRequestItem> items,
        decimal requestedDiscountPercentage,
        string? comments = null)
    {
        ValidateDiscountPercentage(requestedDiscountPercentage);

        if (items == null || !items.Any())
            throw new ArgumentException("Discount request must have at least one item", nameof(items));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        SalespersonId = salespersonId;
        CompanyId = companyId;
        RequestedDiscountPercentage = requestedDiscountPercentage;
        Comments = comments;
        Status = DiscountRequestStatus.UnderAnalysis; // New requests start under analysis
        CreatedAt = DateTime.UtcNow;

        _items.AddRange(items);
    }

    /// <summary>
    /// Approves the discount request
    /// </summary>
    public void Approve()
    {
        if (Status == DiscountRequestStatus.Approved)
            throw new InvalidOperationException("Discount request is already approved");

        if (Status == DiscountRequestStatus.Rejected)
            throw new InvalidOperationException("Cannot approve a rejected discount request");

        Status = DiscountRequestStatus.Approved;
        DecisionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Rejects the discount request
    /// </summary>
    public void Reject()
    {
        if (Status == DiscountRequestStatus.Rejected)
            throw new InvalidOperationException("Discount request is already rejected");

        if (Status == DiscountRequestStatus.Approved || Status == DiscountRequestStatus.AutoApprovedByAI)
            throw new InvalidOperationException("Cannot reject an approved discount request");

        Status = DiscountRequestStatus.Rejected;
        DecisionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Auto-approves the discount request by AI
    /// </summary>
    public void AutoApproveByAI()
    {
        if (Status == DiscountRequestStatus.Approved || Status == DiscountRequestStatus.AutoApprovedByAI)
            throw new InvalidOperationException("Discount request is already approved");

        if (Status == DiscountRequestStatus.Rejected)
            throw new InvalidOperationException("Cannot auto-approve a rejected discount request");

        Status = DiscountRequestStatus.AutoApprovedByAI;
        DecisionAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Requests adjustment to the discount request
    /// </summary>
    public void RequestAdjustment()
    {
        if (Status == DiscountRequestStatus.Approved || Status == DiscountRequestStatus.AutoApprovedByAI)
            throw new InvalidOperationException("Cannot request adjustment for an approved discount request");

        if (Status == DiscountRequestStatus.Rejected)
            throw new InvalidOperationException("Cannot request adjustment for a rejected discount request");

        Status = DiscountRequestStatus.AdjustmentRequested;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the request to under analysis status (after adjustments)
    /// </summary>
    public void ReturnToAnalysis()
    {
        if (Status != DiscountRequestStatus.AdjustmentRequested)
            throw new InvalidOperationException("Only requests with adjustment requested can be returned to analysis");

        Status = DiscountRequestStatus.UnderAnalysis;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the risk score calculated by AI
    /// </summary>
    /// <param name="riskScore">Risk score (0-100)</param>
    public void SetRiskScore(decimal riskScore)
    {
        if (riskScore < 0 || riskScore > 100)
            throw new ArgumentException("Risk score must be between 0 and 100", nameof(riskScore));

        RiskScore = riskScore;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the estimated margin percentage
    /// </summary>
    /// <param name="estimatedMarginPercentage">Estimated margin (0-100)</param>
    public void SetEstimatedMarginPercentage(decimal estimatedMarginPercentage)
    {
        if (estimatedMarginPercentage < 0 || estimatedMarginPercentage > 100)
            throw new ArgumentException("Estimated margin percentage must be between 0 and 100", nameof(estimatedMarginPercentage));

        EstimatedMarginPercentage = estimatedMarginPercentage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the requested discount percentage
    /// </summary>
    /// <param name="requestedDiscountPercentage">New discount percentage</param>
    public void UpdateRequestedDiscountPercentage(decimal requestedDiscountPercentage)
    {
        ValidateDiscountPercentage(requestedDiscountPercentage);
        RequestedDiscountPercentage = requestedDiscountPercentage;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the comments
    /// </summary>
    /// <param name="comments">New comments</param>
    public void UpdateComments(string? comments)
    {
        Comments = comments;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds an item to the discount request
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddItem(DiscountRequestItem item)
    {
        if (Status != DiscountRequestStatus.UnderAnalysis && Status != DiscountRequestStatus.AdjustmentRequested)
            throw new InvalidOperationException("Can only add items to requests under analysis or adjustment requested");

        if (item == null)
            throw new ArgumentNullException(nameof(item));

        // Check if product already exists
        if (_items.Any(i => i.ProductId == item.ProductId))
            throw new InvalidOperationException($"Product {item.ProductName} is already in the request");

        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an item from the discount request
    /// </summary>
    /// <param name="productId">Product ID to remove</param>
    public void RemoveItem(Guid productId)
    {
        if (Status != DiscountRequestStatus.UnderAnalysis && Status != DiscountRequestStatus.AdjustmentRequested)
            throw new InvalidOperationException("Can only remove items from requests under analysis or adjustment requested");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
            throw new InvalidOperationException("Product not found in the request");

        _items.Remove(item);

        if (!_items.Any())
            throw new InvalidOperationException("Cannot remove the last item. Discount request must have at least one item");

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the total base price (sum of all items base prices)
    /// </summary>
    public Money GetTotalBasePrice()
    {
        if (!_items.Any())
            return Money.Zero();

        return _items.Select(i => i.GetTotalBasePrice())
                    .Aggregate((a, b) => a + b);
    }

    /// <summary>
    /// Gets the total final price (sum of all items final prices)
    /// </summary>
    public Money GetTotalFinalPrice()
    {
        if (!_items.Any())
            return Money.Zero();

        return _items.Select(i => i.GetTotalFinalPrice())
                    .Aggregate((a, b) => a + b);
    }

    /// <summary>
    /// Gets the total discount amount
    /// </summary>
    public Money GetTotalDiscountAmount()
    {
        return GetTotalBasePrice() - GetTotalFinalPrice();
    }

    /// <summary>
    /// Checks if the request is pending approval
    /// </summary>
    public bool IsPendingApproval() => Status == DiscountRequestStatus.UnderAnalysis;

    /// <summary>
    /// Checks if the request is approved (manually or by AI)
    /// </summary>
    public bool IsApproved() => Status == DiscountRequestStatus.Approved || Status == DiscountRequestStatus.AutoApprovedByAI;

    /// <summary>
    /// Checks if the request was auto-approved by AI
    /// </summary>
    public bool WasAutoApprovedByAI() => Status == DiscountRequestStatus.AutoApprovedByAI;

    /// <summary>
    /// Checks if the request is rejected
    /// </summary>
    public bool IsRejected() => Status == DiscountRequestStatus.Rejected;

    /// <summary>
    /// Checks if the request requires adjustment
    /// </summary>
    public bool RequiresAdjustment() => Status == DiscountRequestStatus.AdjustmentRequested;

    /// <summary>
    /// Checks if the risk score is high (above threshold)
    /// </summary>
    /// <param name="threshold">Risk score threshold (default 70)</param>
    /// <returns>True if risk score is above threshold</returns>
    public bool HasHighRisk(decimal threshold = 70)
    {
        return RiskScore.HasValue && RiskScore.Value > threshold;
    }

    private static void ValidateDiscountPercentage(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ArgumentException("Discount percentage must be between 0 and 100", nameof(discountPercentage));
    }
}
