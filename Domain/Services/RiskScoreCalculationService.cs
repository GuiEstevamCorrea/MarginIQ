using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Domain service for calculating risk scores for discount requests.
/// Risk score is calculated based on:
/// - Customer history
/// - Discount deviation from standard
/// - Salesperson behavior
/// - Resulting margin
/// High score → requires human approval
/// </summary>
public class RiskScoreCalculationService
{
    private readonly MarginCalculationService _marginCalculationService;

    // Risk score thresholds (0-100 scale)
    private const decimal LowRiskThreshold = 30m;
    private const decimal MediumRiskThreshold = 60m;
    private const decimal HighRiskThreshold = 85m;

    // Weight factors for risk calculation
    private const decimal CustomerHistoryWeight = 0.25m;
    private const decimal DiscountDeviationWeight = 0.35m;
    private const decimal SalespersonBehaviorWeight = 0.15m;
    private const decimal MarginImpactWeight = 0.25m;

    public RiskScoreCalculationService(MarginCalculationService marginCalculationService)
    {
        _marginCalculationService = marginCalculationService ?? throw new ArgumentNullException(nameof(marginCalculationService));
    }

    /// <summary>
    /// Calculates the overall risk score for a discount request
    /// </summary>
    /// <param name="discountRequest">Discount request to evaluate</param>
    /// <param name="customer">Customer associated with the request</param>
    /// <param name="salesperson">Salesperson who created the request</param>
    /// <param name="customerHistory">Customer's discount history</param>
    /// <param name="salespersonHistory">Salesperson's discount history</param>
    /// <param name="productCosts">Dictionary of product costs</param>
    /// <returns>Risk score (0-100)</returns>
    public decimal CalculateRiskScore(
        DiscountRequest discountRequest,
        Customer customer,
        User salesperson,
        CustomerDiscountHistory customerHistory,
        SalespersonDiscountHistory salespersonHistory,
        IDictionary<Guid, Money> productCosts)
    {
        if (discountRequest == null)
            throw new ArgumentNullException(nameof(discountRequest));

        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        if (salesperson == null)
            throw new ArgumentNullException(nameof(salesperson));

        // Calculate individual risk components
        var customerRisk = CalculateCustomerHistoryRisk(customer, customerHistory);
        var discountDeviationRisk = CalculateDiscountDeviationRisk(discountRequest, customerHistory);
        var salespersonRisk = CalculateSalespersonBehaviorRisk(salesperson, salespersonHistory);
        var marginRisk = CalculateMarginImpactRisk(discountRequest, productCosts);

        // Calculate weighted average
        var totalScore =
            (customerRisk * CustomerHistoryWeight) +
            (discountDeviationRisk * DiscountDeviationWeight) +
            (salespersonRisk * SalespersonBehaviorWeight) +
            (marginRisk * MarginImpactWeight);

        // Ensure score is within bounds
        return Math.Clamp(totalScore, 0m, 100m);
    }

    /// <summary>
    /// Calculates risk based on customer history
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="history">Customer's discount history</param>
    /// <returns>Risk score component (0-100)</returns>
    public decimal CalculateCustomerHistoryRisk(Customer customer, CustomerDiscountHistory? history)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        var riskScore = 0m;

        // New or prospect customers have higher risk
        if (customer.IsProspect() || history == null || history.TotalRequests == 0)
        {
            return 70m; // High risk for new customers
        }

        // Calculate rejection rate risk
        var rejectionRate = history.RejectedRequests / (decimal)history.TotalRequests;
        if (rejectionRate > 0.5m)
            riskScore += 40m; // High rejection history
        else if (rejectionRate > 0.3m)
            riskScore += 25m; // Moderate rejection history
        else if (rejectionRate > 0.15m)
            riskScore += 10m; // Low rejection history

        // Calculate payment/default history risk
        if (history.HasPaymentDelays)
            riskScore += 20m;

        if (history.HasDefaults)
            riskScore += 30m;

        // Customer classification factor
        if (customer.IsClassificationA())
            riskScore -= 10m; // Lower risk for A-tier customers
        else if (customer.IsClassificationC())
            riskScore += 10m; // Higher risk for C-tier customers

        // Customer status factor
        if (!customer.IsActive())
            riskScore += 20m;

        return Math.Clamp(riskScore, 0m, 100m);
    }

    /// <summary>
    /// Calculates risk based on discount deviation from customer's standard
    /// </summary>
    /// <param name="discountRequest">Discount request</param>
    /// <param name="history">Customer's discount history</param>
    /// <returns>Risk score component (0-100)</returns>
    public decimal CalculateDiscountDeviationRisk(DiscountRequest discountRequest, CustomerDiscountHistory? history)
    {
        if (discountRequest == null)
            throw new ArgumentNullException(nameof(discountRequest));

        var requestedDiscount = discountRequest.RequestedDiscountPercentage;

        // If no history, any significant discount is risky
        if (history == null || history.TotalRequests == 0)
        {
            if (requestedDiscount > 30m)
                return 90m;
            if (requestedDiscount > 20m)
                return 70m;
            if (requestedDiscount > 10m)
                return 50m;
            return 30m;
        }

        var averageDiscount = history.AverageApprovedDiscount;
        var maxDiscount = history.MaxApprovedDiscount;

        // Calculate deviation from average
        var deviationFromAverage = Math.Abs(requestedDiscount - averageDiscount);
        var deviationPercentage = averageDiscount > 0
            ? (deviationFromAverage / averageDiscount) * 100m
            : requestedDiscount * 10m;

        var riskScore = 0m;

        // Risk increases with deviation
        if (deviationPercentage > 100m) // More than double the average
            riskScore = 90m;
        else if (deviationPercentage > 75m)
            riskScore = 75m;
        else if (deviationPercentage > 50m)
            riskScore = 60m;
        else if (deviationPercentage > 25m)
            riskScore = 40m;
        else
            riskScore = 20m;

        // Additional risk if exceeding historical maximum
        if (requestedDiscount > maxDiscount)
        {
            var excessPercentage = ((requestedDiscount - maxDiscount) / maxDiscount) * 100m;
            if (excessPercentage > 50m)
                riskScore += 30m;
            else if (excessPercentage > 25m)
                riskScore += 20m;
            else
                riskScore += 10m;
        }

        return Math.Clamp(riskScore, 0m, 100m);
    }

    /// <summary>
    /// Calculates risk based on salesperson behavior
    /// </summary>
    /// <param name="salesperson">Salesperson</param>
    /// <param name="history">Salesperson's discount history</param>
    /// <returns>Risk score component (0-100)</returns>
    public decimal CalculateSalespersonBehaviorRisk(User salesperson, SalespersonDiscountHistory? history)
    {
        if (salesperson == null)
            throw new ArgumentNullException(nameof(salesperson));

        var riskScore = 0m;

        // New salespeople have moderate risk
        if (history == null || history.TotalRequests == 0)
        {
            return 50m; // Moderate risk for new salespeople
        }

        // Calculate approval rate
        var approvalRate = history.ApprovedRequests / (decimal)history.TotalRequests;

        // Very high approval rate might indicate over-discounting
        if (approvalRate > 0.95m)
            riskScore += 30m;
        else if (approvalRate > 0.85m)
            riskScore += 15m;

        // Very low approval rate indicates poor judgment
        if (approvalRate < 0.50m)
            riskScore += 35m;
        else if (approvalRate < 0.65m)
            riskScore += 20m;

        // Check for aggressive discounting pattern
        if (history.AverageRequestedDiscount > 25m)
            riskScore += 25m;
        else if (history.AverageRequestedDiscount > 15m)
            riskScore += 15m;

        // Check win rate after discount approval
        if (history.WinRate < 0.60m) // Low conversion despite discounts
            riskScore += 20m;
        else if (history.WinRate > 0.85m) // High conversion (good sign)
            riskScore -= 15m;

        // Recent rejection trend
        if (history.RecentRejectionTrend > 0.40m)
            riskScore += 20m;

        return Math.Clamp(riskScore, 0m, 100m);
    }

    /// <summary>
    /// Calculates risk based on resulting margin impact
    /// </summary>
    /// <param name="discountRequest">Discount request</param>
    /// <param name="productCosts">Dictionary of product costs</param>
    /// <returns>Risk score component (0-100)</returns>
    public decimal CalculateMarginImpactRisk(DiscountRequest discountRequest, IDictionary<Guid, Money>? productCosts)
    {
        if (discountRequest == null)
            throw new ArgumentNullException(nameof(discountRequest));

        // If no cost data available, use estimated margin from request
        if (productCosts == null || !productCosts.Any())
        {
            var estimatedMargin = discountRequest.EstimatedMarginPercentage ?? 0m;
            return CalculateMarginRisk(estimatedMargin);
        }

        var totalRisk = 0m;
        var itemCount = 0;

        foreach (var item in discountRequest.Items)
        {
            if (!productCosts.TryGetValue(item.ProductId, out var productCost))
                continue;

            var finalPrice = item.UnitFinalPrice;
            var margin = _marginCalculationService.CalculateMarginPercentage(finalPrice, productCost);

            totalRisk += CalculateMarginRisk(margin);
            itemCount++;
        }

        return itemCount > 0 ? totalRisk / itemCount : 50m;
    }

    /// <summary>
    /// Calculates risk based on margin percentage
    /// </summary>
    private decimal CalculateMarginRisk(decimal marginPercentage)
    {
        if (marginPercentage < 0m)
            return 100m; // Negative margin = maximum risk

        if (marginPercentage < 5m)
            return 95m; // Very low margin

        if (marginPercentage < 10m)
            return 80m;

        if (marginPercentage < 15m)
            return 60m;

        if (marginPercentage < 20m)
            return 40m;

        if (marginPercentage < 25m)
            return 25m;

        if (marginPercentage < 30m)
            return 15m;

        return 5m; // Good margin = low risk
    }

    /// <summary>
    /// Determines risk level based on score
    /// </summary>
    /// <param name="riskScore">Risk score (0-100)</param>
    /// <returns>Risk level</returns>
    public RiskLevel DetermineRiskLevel(decimal riskScore)
    {
        if (riskScore >= HighRiskThreshold)
            return RiskLevel.High;

        if (riskScore >= MediumRiskThreshold)
            return RiskLevel.Medium;

        if (riskScore >= LowRiskThreshold)
            return RiskLevel.Low;

        return RiskLevel.VeryLow;
    }

    /// <summary>
    /// Determines if human approval is required based on risk score
    /// </summary>
    /// <param name="riskScore">Risk score (0-100)</param>
    /// <returns>True if human approval is required</returns>
    public bool RequiresHumanApproval(decimal riskScore)
    {
        return riskScore >= MediumRiskThreshold;
    }

    /// <summary>
    /// Gets a detailed risk assessment explanation
    /// </summary>
    /// <param name="discountRequest">Discount request</param>
    /// <param name="riskScore">Calculated risk score</param>
    /// <param name="customerHistory">Customer history</param>
    /// <param name="salespersonHistory">Salesperson history</param>
    /// <returns>Risk assessment details</returns>
    public RiskAssessment GetRiskAssessment(
        DiscountRequest discountRequest,
        decimal riskScore,
        CustomerDiscountHistory? customerHistory,
        SalespersonDiscountHistory? salespersonHistory)
    {
        var level = DetermineRiskLevel(riskScore);
        var reasons = new List<string>();

        // Analyze customer factors
        if (customerHistory == null || customerHistory.TotalRequests == 0)
        {
            reasons.Add("New customer with no discount history");
        }
        else if (customerHistory.RejectedRequests / (decimal)customerHistory.TotalRequests > 0.3m)
        {
            reasons.Add("Customer has high rejection rate in history");
        }

        // Analyze discount deviation
        if (customerHistory != null && customerHistory.TotalRequests > 0)
        {
            var avgDiscount = customerHistory.AverageApprovedDiscount;
            if (discountRequest.RequestedDiscountPercentage > avgDiscount * 1.5m)
            {
                reasons.Add($"Requested discount significantly exceeds customer average ({avgDiscount:N1}%)");
            }
        }

        // Analyze margin
        if (discountRequest.EstimatedMarginPercentage.HasValue)
        {
            var margin = discountRequest.EstimatedMarginPercentage.Value;
            if (margin < 10m)
            {
                reasons.Add($"Very low resulting margin ({margin:N1}%)");
            }
            else if (margin < 15m)
            {
                reasons.Add($"Low resulting margin ({margin:N1}%)");
            }
        }

        // Analyze salesperson
        if (salespersonHistory != null && salespersonHistory.TotalRequests > 0)
        {
            var approvalRate = salespersonHistory.ApprovedRequests / (decimal)salespersonHistory.TotalRequests;
            if (approvalRate < 0.60m)
            {
                reasons.Add("Salesperson has low approval rate history");
            }
        }

        return new RiskAssessment
        {
            Score = riskScore,
            Level = level,
            RequiresHumanApproval = RequiresHumanApproval(riskScore),
            Reasons = reasons
        };
    }
}

/// <summary>
/// Risk level classification
/// </summary>
public enum RiskLevel
{
    VeryLow = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// Customer discount history data
/// </summary>
public class CustomerDiscountHistory
{
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public decimal AverageApprovedDiscount { get; set; }
    public decimal MaxApprovedDiscount { get; set; }
    public bool HasPaymentDelays { get; set; }
    public bool HasDefaults { get; set; }
}

/// <summary>
/// Salesperson discount history data
/// </summary>
public class SalespersonDiscountHistory
{
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public decimal AverageRequestedDiscount { get; set; }
    public decimal WinRate { get; set; } // Conversion rate after discount approval
    public decimal RecentRejectionTrend { get; set; } // Recent rejection rate (last 30 days)
}

/// <summary>
/// Risk assessment result
/// </summary>
public class RiskAssessment
{
    public decimal Score { get; set; }
    public RiskLevel Level { get; set; }
    public bool RequiresHumanApproval { get; set; }
    public List<string> Reasons { get; set; } = new();
}
