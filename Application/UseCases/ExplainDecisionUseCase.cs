using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case: Explain AI Decision (IA-UC-03 – Explicabilidade)
/// Generates simple text explanations for AI decisions regarding discount recommendations,
/// risk scores, and approval decisions.
/// Examples: "Discount is common for this customer", "Margin below historical standard"
/// </summary>
public class ExplainDecisionUseCase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAIService _aiService;

    public ExplainDecisionUseCase(
        ICustomerRepository customerRepository,
        IDiscountRequestRepository discountRequestRepository,
        IProductRepository productRepository,
        IAIService aiService)
    {
        _customerRepository = customerRepository;
        _discountRequestRepository = discountRequestRepository;
        _productRepository = productRepository;
        _aiService = aiService;
    }

    /// <summary>
    /// Generates explanation for a discount/approval decision
    /// </summary>
    public async Task<ExplainDecisionResponse> ExecuteAsync(
        ExplainDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Gather context for explanation
        var context = await GatherContextAsync(request, cancellationToken);

        // Try AI-based explanation first if requested
        if (request.UseAI && await _aiService.IsAvailableAsync(request.CompanyId, cancellationToken))
        {
            try
            {
                var aiExplanation = await GetAIExplanationAsync(request, context, cancellationToken);
                if (aiExplanation != null)
                {
                    return aiExplanation;
                }
            }
            catch
            {
                // Fall through to rule-based explanation
            }
        }

        // Fallback to rule-based explanation
        return GenerateRuleBasedExplanation(request, context);
    }

    /// <summary>
    /// Gets explanation from AI service
    /// </summary>
    private async Task<ExplainDecisionResponse?> GetAIExplanationAsync(
        ExplainDecisionRequest request,
        ExplanationContext context,
        CancellationToken cancellationToken)
    {
        var aiRequest = new ExplainabilityRequest
        {
            CompanyId = request.CompanyId,
            DiscountRequestId = request.DiscountRequestId ?? Guid.Empty,
            RecommendedDiscount = request.RecommendedDiscount ?? 0,
            RiskScore = request.RiskScore ?? 0,
            WasAutoApproved = request.WasAutoApproved ?? false
        };

        var aiResponse = await _aiService.ExplainDecisionAsync(aiRequest, cancellationToken);

        if (aiResponse == null)
        {
            return null;
        }

        return new ExplainDecisionResponse
        {
            Summary = aiResponse.Summary,
            Details = aiResponse.Details ?? new List<string>(),
            KeyFactors = new List<ExplanationFactor>(),
            PositiveIndicators = new List<string>(),
            Concerns = new List<string>(),
            Recommendations = aiResponse.Recommendations ?? new List<string>(),
            Confidence = 0.9m,
            Source = "AI",
            IsFallback = false,
            Type = request.Type,
            GeneratedAt = aiResponse.GeneratedAt
        };
    }

    /// <summary>
    /// Generates rule-based explanation using templates
    /// </summary>
    private ExplainDecisionResponse GenerateRuleBasedExplanation(
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        var response = new ExplainDecisionResponse
        {
            Source = "RuleBased",
            IsFallback = true,
            Type = request.Type,
            GeneratedAt = DateTime.UtcNow
        };

        // Generate summary based on explanation type
        response.Summary = GenerateSummary(request, context);

        // Add details
        if (request.IncludeDetails)
        {
            AddDetailedExplanation(response, request, context);
        }

        // Add key factors
        AddKeyFactors(response, request, context);

        // Add positive indicators and concerns
        AddPositiveIndicatorsAndConcerns(response, request, context);

        // Add recommendations
        AddRecommendations(response, request, context);

        // Set confidence based on data quality
        response.Confidence = CalculateConfidence(context);

        return response;
    }

    /// <summary>
    /// Generates simple summary explanation (e.g., "Discount is common for this customer")
    /// </summary>
    private string GenerateSummary(ExplainDecisionRequest request, ExplanationContext context)
    {
        switch (request.Type)
        {
            case ExplanationType.DiscountRecommendation:
                return GenerateDiscountRecommendationSummary(request, context);

            case ExplanationType.RiskScore:
                return GenerateRiskScoreSummary(request, context);

            case ExplanationType.AutoApproval:
                return GenerateAutoApprovalSummary(request, context);

            case ExplanationType.Rejection:
                return GenerateRejectionSummary(request, context);

            default: // General
                return GenerateGeneralSummary(request, context);
        }
    }

    private string GenerateDiscountRecommendationSummary(
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        if (request.RecommendedDiscount == null)
        {
            return "No discount recommendation available";
        }

        var discount = request.RecommendedDiscount.Value;
        var avgDiscount = context.CustomerHistoricalData.AverageDiscount;

        // Compare with customer's historical discount
        if (avgDiscount > 0)
        {
            if (Math.Abs(discount - avgDiscount) <= 2) // Within 2% of average
            {
                return $"Discount is common for this customer (historical average: {avgDiscount:F1}%)";
            }
            else if (discount < avgDiscount)
            {
                return $"Conservative discount compared to customer's history (usually {avgDiscount:F1}%)";
            }
            else
            {
                return $"Higher than usual discount for this customer (historical average: {avgDiscount:F1}%)";
            }
        }

        // Check customer classification
        if (context.Customer != null)
        {
            if (context.Customer.IsClassificationA())
            {
                return $"Competitive discount for Class A customer (top tier)";
            }
            else if (context.Customer.IsClassificationB())
            {
                return $"Standard discount for Class B customer (mid tier)";
            }
            else
            {
                return $"Minimal discount for Class C customer (entry level)";
            }
        }

        return $"Recommended discount: {discount:F1}%";
    }

    private string GenerateRiskScoreSummary(ExplainDecisionRequest request, ExplanationContext context)
    {
        if (request.RiskScore == null)
        {
            return "No risk assessment available";
        }

        var riskScore = request.RiskScore.Value;

        if (riskScore < 30)
        {
            return "Low risk: Customer has excellent history and discount is within normal parameters";
        }
        else if (riskScore < 60)
        {
            return "Moderate risk: Standard approval process recommended";
        }
        else
        {
            var reasons = new List<string>();

            if (context.CustomerHistoricalData.HasPaymentIssues)
            {
                reasons.Add("payment history concerns");
            }

            if (request.RecommendedDiscount > context.CustomerHistoricalData.AverageDiscount + 5)
            {
                reasons.Add("discount above historical pattern");
            }

            if (context.RequestedMargin < context.ProductHistoricalData.AverageMargin - 5)
            {
                reasons.Add("margin below historical standard");
            }

            if (reasons.Any())
            {
                return $"High risk: {string.Join(", ", reasons)}";
            }

            return "High risk: Manager approval recommended";
        }
    }

    private string GenerateAutoApprovalSummary(ExplainDecisionRequest request, ExplanationContext context)
    {
        if (request.WasAutoApproved == true)
        {
            return "Automatically approved: Low risk, meets all business rules, within historical patterns";
        }
        else
        {
            return "Requires human approval: Risk level or discount amount exceeds auto-approval thresholds";
        }
    }

    private string GenerateRejectionSummary(ExplainDecisionRequest request, ExplanationContext context)
    {
        var reasons = new List<string>();

        if (context.CustomerHistoricalData.HasPaymentIssues)
        {
            reasons.Add("customer has payment issues");
        }

        if (request.RecommendedDiscount > 30)
        {
            reasons.Add("discount exceeds maximum allowed");
        }

        if (context.RequestedMargin < 10)
        {
            reasons.Add("margin below minimum threshold");
        }

        if (reasons.Any())
        {
            return $"Rejected: {string.Join(", ", reasons)}";
        }

        return "Rejected: Does not meet approval criteria";
    }

    private string GenerateGeneralSummary(ExplainDecisionRequest request, ExplanationContext context)
    {
        if (request.RiskScore != null)
        {
            return GenerateRiskScoreSummary(request, context);
        }

        if (request.RecommendedDiscount != null)
        {
            return GenerateDiscountRecommendationSummary(request, context);
        }

        return "Decision analysis completed";
    }

    /// <summary>
    /// Adds detailed explanation points
    /// </summary>
    private void AddDetailedExplanation(
        ExplainDecisionResponse response,
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        // Customer context
        if (context.Customer != null)
        {
            response.Details.Add($"Customer: {context.Customer.Name} (Classification: {context.Customer.Classification})");

            if (context.CustomerHistoricalData.RequestCount > 0)
            {
                response.Details.Add($"Historical requests: {context.CustomerHistoricalData.RequestCount} " +
                    $"(Avg discount: {context.CustomerHistoricalData.AverageDiscount:F1}%, " +
                    $"Approval rate: {context.CustomerHistoricalData.ApprovalRate:F0}%)");
            }
        }

        // Discount analysis
        if (request.RecommendedDiscount != null)
        {
            response.Details.Add($"Recommended discount: {request.RecommendedDiscount:F1}%");

            var deviation = request.RecommendedDiscount.Value - context.CustomerHistoricalData.AverageDiscount;
            if (Math.Abs(deviation) > 2)
            {
                response.Details.Add($"Deviation from customer average: {deviation:+0.0;-0.0}%");
            }
        }

        // Risk analysis
        if (request.RiskScore != null)
        {
            response.Details.Add($"Risk score: {request.RiskScore:F0}/100");
        }

        // Margin analysis
        if (context.RequestedMargin > 0)
        {
            response.Details.Add($"Estimated margin: {context.RequestedMargin:F1}%");

            if (context.ProductHistoricalData.AverageMargin > 0)
            {
                var marginDiff = context.RequestedMargin - context.ProductHistoricalData.AverageMargin;
                if (Math.Abs(marginDiff) > 2)
                {
                    response.Details.Add($"Margin vs. product average: {marginDiff:+0.0;-0.0}%");
                }
            }
        }
    }

    /// <summary>
    /// Adds key factors that influenced the decision
    /// </summary>
    private void AddKeyFactors(
        ExplainDecisionResponse response,
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        // Customer classification factor
        if (context.Customer != null)
        {
            var classificationImpact = context.Customer.IsClassificationA() ? "Positive" :
                                      context.Customer.IsClassificationB() ? "Neutral" : "Negative";

            response.KeyFactors.Add(new ExplanationFactor
            {
                Name = "Customer Classification",
                Description = $"Customer is classified as {context.Customer.Classification}",
                Impact = classificationImpact,
                Weight = 0.25m
            });
        }

        // Discount deviation factor
        if (request.RecommendedDiscount != null && context.CustomerHistoricalData.AverageDiscount > 0)
        {
            var deviation = Math.Abs(request.RecommendedDiscount.Value - context.CustomerHistoricalData.AverageDiscount);
            var impact = deviation <= 2 ? "Positive" : deviation <= 5 ? "Neutral" : "Negative";

            response.KeyFactors.Add(new ExplanationFactor
            {
                Name = "Discount Consistency",
                Description = $"Discount is {deviation:F1}% from customer's historical average",
                Impact = impact,
                Weight = 0.35m
            });
        }

        // Payment history factor
        if (context.CustomerHistoricalData.RequestCount > 0)
        {
            var impact = context.CustomerHistoricalData.HasPaymentIssues ? "Negative" : "Positive";

            response.KeyFactors.Add(new ExplanationFactor
            {
                Name = "Payment History",
                Description = context.CustomerHistoricalData.HasPaymentIssues ?
                    "Customer has payment issues" : "Customer has good payment history",
                Impact = impact,
                Weight = 0.20m
            });
        }

        // Margin factor
        if (context.RequestedMargin > 0)
        {
            var impact = context.RequestedMargin >= 20 ? "Positive" :
                        context.RequestedMargin >= 10 ? "Neutral" : "Negative";

            response.KeyFactors.Add(new ExplanationFactor
            {
                Name = "Margin Impact",
                Description = $"Estimated margin: {context.RequestedMargin:F1}%",
                Impact = impact,
                Weight = 0.20m
            });
        }
    }

    /// <summary>
    /// Adds positive indicators and concerns
    /// </summary>
    private void AddPositiveIndicatorsAndConcerns(
        ExplainDecisionResponse response,
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        // Positive indicators
        if (context.Customer?.IsClassificationA() == true)
        {
            response.PositiveIndicators.Add("Top-tier customer (Class A)");
        }

        if (context.CustomerHistoricalData.ApprovalRate >= 80)
        {
            response.PositiveIndicators.Add($"High approval rate ({context.CustomerHistoricalData.ApprovalRate:F0}%)");
        }

        if (!context.CustomerHistoricalData.HasPaymentIssues)
        {
            response.PositiveIndicators.Add("Good payment history");
        }

        if (request.RiskScore < 30)
        {
            response.PositiveIndicators.Add("Low risk score");
        }

        if (context.RequestedMargin >= 20)
        {
            response.PositiveIndicators.Add("Healthy margin maintained");
        }

        // Concerns
        if (context.CustomerHistoricalData.HasPaymentIssues)
        {
            response.Concerns.Add("Customer has payment issues");
        }

        if (request.RecommendedDiscount > context.CustomerHistoricalData.AverageDiscount + 5)
        {
            response.Concerns.Add("Discount significantly above customer's historical average");
        }

        if (context.RequestedMargin < 10)
        {
            response.Concerns.Add("Margin below minimum threshold");
        }

        if (request.RiskScore >= 60)
        {
            response.Concerns.Add("High risk score");
        }

        if (context.CustomerHistoricalData.ApprovalRate < 50)
        {
            response.Concerns.Add("Low historical approval rate");
        }
    }

    /// <summary>
    /// Adds recommendations based on the analysis
    /// </summary>
    private void AddRecommendations(
        ExplainDecisionResponse response,
        ExplainDecisionRequest request,
        ExplanationContext context)
    {
        if (request.RiskScore >= 60)
        {
            response.Recommendations.Add("Manager approval required due to high risk");
        }
        else if (request.RiskScore >= 30)
        {
            response.Recommendations.Add("Standard approval process recommended");
        }
        else
        {
            response.Recommendations.Add("Eligible for auto-approval");
        }

        if (context.CustomerHistoricalData.HasPaymentIssues)
        {
            response.Recommendations.Add("Consider payment terms adjustment");
        }

        if (context.RequestedMargin < 15)
        {
            response.Recommendations.Add("Review margin impact with finance team");
        }

        if (request.RecommendedDiscount > 25)
        {
            response.Recommendations.Add("Consider multi-level approval for high discount");
        }
    }

    /// <summary>
    /// Calculates confidence based on data quality
    /// </summary>
    private decimal CalculateConfidence(ExplanationContext context)
    {
        decimal confidence = 0.5m; // Base confidence

        // More historical data = higher confidence
        if (context.CustomerHistoricalData.RequestCount >= 10)
        {
            confidence += 0.3m;
        }
        else if (context.CustomerHistoricalData.RequestCount >= 5)
        {
            confidence += 0.2m;
        }
        else if (context.CustomerHistoricalData.RequestCount > 0)
        {
            confidence += 0.1m;
        }

        // Customer classification adds confidence
        if (context.Customer != null)
        {
            confidence += 0.1m;
        }

        // Product data adds confidence
        if (context.ProductHistoricalData.AverageMargin > 0)
        {
            confidence += 0.1m;
        }

        return Math.Min(confidence, 1.0m);
    }

    /// <summary>
    /// Gathers all context needed for explanation
    /// </summary>
    private async Task<ExplanationContext> GatherContextAsync(
        ExplainDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ExplanationContext();

        // Get customer
        context.Customer = await _customerRepository.GetByIdAsync(
            request.CustomerId,
            cancellationToken);

        // Get discount request if available
        if (request.DiscountRequestId.HasValue)
        {
            context.DiscountRequest = await _discountRequestRepository.GetByIdAsync(
                request.DiscountRequestId.Value,
                cancellationToken);

            if (context.DiscountRequest != null)
            {
                context.RequestedMargin = context.DiscountRequest.EstimatedMarginPercentage ?? 0;
            }
        }

        // Get customer historical data
        context.CustomerHistoricalData = await GatherCustomerHistoricalDataAsync(
            request.CompanyId,
            request.CustomerId,
            cancellationToken);

        // Get product historical data (simplified - would need product IDs from request)
        context.ProductHistoricalData = new ProductHistoricalData
        {
            AverageMargin = 25 // Default assumption
        };

        return context;
    }

    /// <summary>
    /// Gathers customer historical data for analysis
    /// </summary>
    private async Task<CustomerHistoricalData> GatherCustomerHistoricalDataAsync(
        Guid companyId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var historicalRequests = (await _discountRequestRepository.GetByCustomerIdAsync(
            companyId,
            customerId,
            cancellationToken)).ToList();

        if (!historicalRequests.Any())
        {
            return new CustomerHistoricalData();
        }

        var approvedRequests = historicalRequests
            .Where(r => r.Status == Domain.Enums.DiscountRequestStatus.Approved)
            .ToList();

        return new CustomerHistoricalData
        {
            RequestCount = historicalRequests.Count,
            ApprovedCount = approvedRequests.Count,
            ApprovalRate = historicalRequests.Count > 0
                ? (decimal)approvedRequests.Count / historicalRequests.Count * 100
                : 0,
            AverageDiscount = approvedRequests.Any()
                ? approvedRequests.Average(r => r.RequestedDiscountPercentage)
                : 0,
            HasPaymentIssues = false // Would need to check actual payment data
        };
    }

    private string BuildCustomerHistorySummary(ExplanationContext context)
    {
        if (context.CustomerHistoricalData.RequestCount == 0)
        {
            return "New customer with no discount history";
        }

        return $"Customer has {context.CustomerHistoricalData.RequestCount} historical requests, " +
               $"{context.CustomerHistoricalData.ApprovalRate:F0}% approval rate, " +
               $"average discount {context.CustomerHistoricalData.AverageDiscount:F1}%";
    }
}

/// <summary>
/// Context for generating explanations
/// </summary>
internal class ExplanationContext
{
    public Customer? Customer { get; set; }
    public DiscountRequest? DiscountRequest { get; set; }
    public CustomerHistoricalData CustomerHistoricalData { get; set; } = new();
    public ProductHistoricalData ProductHistoricalData { get; set; } = new();
    public decimal RequestedMargin { get; set; }
}

/// <summary>
/// Customer historical data summary
/// </summary>
internal class CustomerHistoricalData
{
    public int RequestCount { get; set; }
    public int ApprovedCount { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal AverageDiscount { get; set; }
    public bool HasPaymentIssues { get; set; }
}

/// <summary>
/// Product historical data summary
/// </summary>
internal class ProductHistoricalData
{
    public decimal AverageMargin { get; set; }
}
