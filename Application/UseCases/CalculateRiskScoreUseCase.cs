using Application.DTOs;
using Application.Ports;
using Domain.Repositories;
using Domain.Services;
using Domain.Entities;
using Domain.ValueObjects;
using System.Diagnostics;

namespace Application.UseCases;

/// <summary>
/// Use Case: IA-UC-02 - Calculate Risk Score
/// 
/// Analyzes deviation from historical pattern
/// Returns score 0-100
/// 
/// This use case orchestrates the risk score calculation process,
/// using AI when available with fallback to rule-based calculation.
/// </summary>
public class CalculateRiskScoreUseCase
{
    private readonly IAIService _aiService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IProductRepository _productRepository;
    private readonly RiskScoreCalculationService _riskScoreCalculationService;

    // Risk component weights (matching Domain service)
    private const decimal CustomerHistoryWeight = 0.25m;
    private const decimal DiscountDeviationWeight = 0.35m;
    private const decimal SalespersonBehaviorWeight = 0.15m;
    private const decimal MarginImpactWeight = 0.25m;

    public CalculateRiskScoreUseCase(
        IAIService aiService,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IDiscountRequestRepository discountRequestRepository,
        IProductRepository productRepository,
        RiskScoreCalculationService riskScoreCalculationService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _riskScoreCalculationService = riskScoreCalculationService ?? throw new ArgumentNullException(nameof(riskScoreCalculationService));
    }

    /// <summary>
    /// Executes the risk score calculation use case
    /// </summary>
    public async Task<CalculateRiskScoreResponse> ExecuteAsync(
        CalculateRiskScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Validate input
        ValidateRequest(request);

        // Step 2: Load entities
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} not found");

        var salesperson = await _userRepository.GetByIdAsync(request.SalespersonId)
            ?? throw new InvalidOperationException($"Salesperson {request.SalespersonId} not found");

        // Load discount request if ID provided
        DiscountRequest? discountRequest = null;
        if (request.DiscountRequestId.HasValue)
        {
            discountRequest = await _discountRequestRepository.GetByIdAsync(request.DiscountRequestId.Value);
        }

        // Step 3: Gather historical data
        var customerHistory = request.IncludeCustomerHistory
            ? await GatherCustomerHistoryAsync(request.CompanyId, request.CustomerId, cancellationToken)
            : null;

        var salespersonHistory = request.IncludeSalespersonHistory
            ? await GatherSalespersonHistoryAsync(request.CompanyId, request.SalespersonId, cancellationToken)
            : null;

        // Step 4: Attempt AI calculation if enabled
        CalculateRiskScoreResponse response;

        if (request.UseAI && await _aiService.IsAvailableAsync(request.CompanyId, cancellationToken))
        {
            try
            {
                response = await GetAIRiskScoreAsync(
                    request,
                    customer,
                    salesperson,
                    customerHistory,
                    salespersonHistory,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // AI failed - fallback to rule-based
                response = GetRuleBasedRiskScore(
                    request,
                    customer,
                    salesperson,
                    discountRequest,
                    customerHistory,
                    salespersonHistory);

                response.IsFallback = true;
                response.Source = "RuleBased";
                response.Recommendations.Add($"AI service unavailable: {ex.Message}. Using rule-based calculation.");
            }
        }
        else
        {
            // Use rule-based calculation
            response = GetRuleBasedRiskScore(
                request,
                customer,
                salesperson,
                discountRequest,
                customerHistory,
                salespersonHistory);

            response.IsFallback = true;
            response.Source = "RuleBased";
        }

        stopwatch.Stop();
        response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        return response;
    }

    /// <summary>
    /// Gets AI-based risk score
    /// </summary>
    private async Task<CalculateRiskScoreResponse> GetAIRiskScoreAsync(
        CalculateRiskScoreRequest request,
        Customer customer,
        User salesperson,
        CustomerDiscountHistory? customerHistory,
        SalespersonDiscountHistory? salespersonHistory,
        CancellationToken cancellationToken)
    {
        // Build AI request
        var aiRequest = new RiskScoreRequest
        {
            CompanyId = request.CompanyId,
            DiscountRequestId = request.DiscountRequestId ?? Guid.NewGuid(),
            CustomerId = request.CustomerId,
            SalespersonId = request.SalespersonId,
            RequestedDiscountPercentage = request.RequestedDiscountPercentage,
            EstimatedMarginPercentage = request.EstimatedMarginPercentage ?? 0m,
            CustomerHistory = customerHistory != null ? new CustomerHistoryData
            {
                TotalOrders = customerHistory.TotalRequests,
                AverageDiscount = customerHistory.AverageApprovedDiscount,
                MaxDiscountReceived = customerHistory.MaxApprovedDiscount,
                RejectedRequests = customerHistory.RejectedRequests,
                Classification = customer.GetClassificationTier(),
                HasPaymentIssues = customerHistory.HasPaymentDelays || customerHistory.HasDefaults
            } : null,
            SalespersonHistory = salespersonHistory != null ? new SalespersonHistoryData
            {
                TotalRequests = salespersonHistory.TotalRequests,
                ApprovedRequests = salespersonHistory.ApprovedRequests,
                AverageDiscount = salespersonHistory.AverageRequestedDiscount,
                ApprovalRate = salespersonHistory.TotalRequests > 0
                    ? (decimal)salespersonHistory.ApprovedRequests / salespersonHistory.TotalRequests
                    : 0m,
                WinRate = salespersonHistory.WinRate
            } : null
        };

        // Call AI service
        var aiRiskScore = await _aiService.CalculateRiskScoreAsync(aiRequest, cancellationToken);

        // Build response
        var response = new CalculateRiskScoreResponse
        {
            RiskScore = aiRiskScore.Score,
            RiskLevel = aiRiskScore.RiskLevel,
            RiskFactors = aiRiskScore.RiskFactors,
            Confidence = aiRiskScore.Confidence,
            IsFallback = aiRiskScore.IsFallback,
            Source = aiRiskScore.IsFallback ? "RuleBased" : "AI",
            RequiresHumanApproval = _riskScoreCalculationService.RequiresHumanApproval(aiRiskScore.Score)
        };

        // Add recommendations based on risk level
        AddRecommendations(response);

        return response;
    }

    /// <summary>
    /// Gets rule-based risk score (fallback when AI is unavailable)
    /// </summary>
    private CalculateRiskScoreResponse GetRuleBasedRiskScore(
        CalculateRiskScoreRequest request,
        Customer customer,
        User salesperson,
        DiscountRequest? discountRequest,
        CustomerDiscountHistory? customerHistory,
        SalespersonDiscountHistory? salespersonHistory)
    {
        // Calculate individual risk components using Domain service
        var customerRisk = _riskScoreCalculationService.CalculateCustomerHistoryRisk(customer, customerHistory);
        
        var discountDeviationRisk = CalculateDiscountDeviationRiskFallback(
            request.RequestedDiscountPercentage,
            customerHistory);
        
        var salespersonRisk = _riskScoreCalculationService.CalculateSalespersonBehaviorRisk(
            salesperson,
            salespersonHistory);
        
        var marginRisk = CalculateMarginRiskFallback(request.EstimatedMarginPercentage);

        // Calculate weighted score
        var totalScore =
            (customerRisk * CustomerHistoryWeight) +
            (discountDeviationRisk * DiscountDeviationWeight) +
            (salespersonRisk * SalespersonBehaviorWeight) +
            (marginRisk * MarginImpactWeight);

        totalScore = Math.Clamp(totalScore, 0m, 100m);

        var riskLevel = _riskScoreCalculationService.DetermineRiskLevel(totalScore);

        var response = new CalculateRiskScoreResponse
        {
            RiskScore = totalScore,
            RiskLevel = riskLevel.ToString(),
            Confidence = 0.65m, // Medium confidence for rule-based
            Source = "RuleBased",
            IsFallback = true,
            RequiresHumanApproval = _riskScoreCalculationService.RequiresHumanApproval(totalScore),
            Breakdown = new RiskBreakdown
            {
                CustomerHistoryRisk = customerRisk,
                DiscountDeviationRisk = discountDeviationRisk,
                SalespersonBehaviorRisk = salespersonRisk,
                MarginImpactRisk = marginRisk,
                Weights = new Dictionary<string, decimal>
                {
                    { "CustomerHistory", CustomerHistoryWeight },
                    { "DiscountDeviation", DiscountDeviationWeight },
                    { "SalespersonBehavior", SalespersonBehaviorWeight },
                    { "MarginImpact", MarginImpactWeight }
                }
            }
        };

        // Add risk factors
        AddRiskFactors(response, customer, customerHistory, salespersonHistory);

        // Add recommendations
        AddRecommendations(response);

        return response;
    }

    /// <summary>
    /// Calculates discount deviation risk (fallback logic)
    /// </summary>
    private decimal CalculateDiscountDeviationRiskFallback(
        decimal requestedDiscount,
        CustomerDiscountHistory? history)
    {
        // If no history, any significant discount is risky
        if (history == null || history.TotalRequests == 0)
        {
            if (requestedDiscount > 30m) return 90m;
            if (requestedDiscount > 20m) return 70m;
            if (requestedDiscount > 10m) return 50m;
            return 30m;
        }

        var averageDiscount = history.AverageApprovedDiscount;
        var deviation = Math.Abs(requestedDiscount - averageDiscount);
        var deviationPercentage = averageDiscount > 0
            ? (deviation / averageDiscount) * 100m
            : requestedDiscount * 10m;

        if (deviationPercentage > 100m) return 90m;
        if (deviationPercentage > 75m) return 75m;
        if (deviationPercentage > 50m) return 60m;
        if (deviationPercentage > 25m) return 40m;
        return 20m;
    }

    /// <summary>
    /// Calculates margin risk (fallback logic)
    /// </summary>
    private decimal CalculateMarginRiskFallback(decimal? marginPercentage)
    {
        if (!marginPercentage.HasValue) return 50m; // Unknown margin = medium risk

        var margin = marginPercentage.Value;

        if (margin < 0m) return 100m;
        if (margin < 5m) return 95m;
        if (margin < 10m) return 80m;
        if (margin < 15m) return 60m;
        if (margin < 20m) return 40m;
        if (margin < 25m) return 25m;
        if (margin < 30m) return 15m;
        return 5m;
    }

    /// <summary>
    /// Adds risk factors to the response
    /// </summary>
    private void AddRiskFactors(
        CalculateRiskScoreResponse response,
        Customer customer,
        CustomerDiscountHistory? customerHistory,
        SalespersonDiscountHistory? salespersonHistory)
    {
        // Customer factors
        if (customer.IsProspect() || customerHistory == null || customerHistory.TotalRequests == 0)
        {
            response.RiskFactors.Add("New customer with no discount history");
        }
        else if (customerHistory.RejectedRequests / (decimal)customerHistory.TotalRequests > 0.3m)
        {
            response.RiskFactors.Add("High rejection rate in customer history");
        }

        if (customerHistory?.HasPaymentDelays == true)
        {
            response.RiskFactors.Add("Customer has payment delays");
        }

        if (customerHistory?.HasDefaults == true)
        {
            response.RiskFactors.Add("Customer has payment defaults");
        }

        // Salesperson factors
        if (salespersonHistory != null && salespersonHistory.TotalRequests > 0)
        {
            var approvalRate = (decimal)salespersonHistory.ApprovedRequests / salespersonHistory.TotalRequests;
            
            if (approvalRate < 0.60m)
            {
                response.RiskFactors.Add("Salesperson has low approval rate");
            }
            
            if (salespersonHistory.AverageRequestedDiscount > 20m)
            {
                response.RiskFactors.Add("Salesperson tends to request high discounts");
            }
        }

        // Margin factors
        if (response.Breakdown?.MarginImpactRisk > 60m)
        {
            response.RiskFactors.Add("Low expected margin after discount");
        }
    }

    /// <summary>
    /// Adds recommendations based on risk level
    /// </summary>
    private void AddRecommendations(CalculateRiskScoreResponse response)
    {
        switch (response.RiskLevel.ToLower())
        {
            case "high":
                response.Recommendations.Add("Require manager approval before proceeding");
                response.Recommendations.Add("Review customer payment history and credit limit");
                response.Recommendations.Add("Consider reducing discount to lower risk");
                break;

            case "medium":
                response.Recommendations.Add("Standard approval process recommended");
                response.Recommendations.Add("Verify margin calculations before approval");
                break;

            case "low":
            case "verylow":
                response.Recommendations.Add("Low risk - may qualify for auto-approval");
                break;
        }

        if (response.RequiresHumanApproval)
        {
            response.Recommendations.Add("Human review required due to risk level");
        }
    }

    /// <summary>
    /// Gathers customer historical data
    /// </summary>
    private async Task<CustomerDiscountHistory?> GatherCustomerHistoryAsync(
        Guid companyId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var requests = await _discountRequestRepository.GetByCustomerIdAsync(companyId, customerId, cancellationToken);
        var requestsList = requests.ToList();

        if (!requestsList.Any())
            return null;

        var approved = requestsList.Where(r => r.IsApproved()).ToList();

        return new CustomerDiscountHistory
        {
            TotalRequests = requestsList.Count,
            ApprovedRequests = approved.Count,
            RejectedRequests = requestsList.Count(r => r.IsRejected()),
            AverageApprovedDiscount = approved.Any()
                ? approved.Average(r => r.RequestedDiscountPercentage)
                : 0m,
            MaxApprovedDiscount = approved.Any()
                ? approved.Max(r => r.RequestedDiscountPercentage)
                : 0m,
            HasPaymentDelays = false, // Would need payment system integration
            HasDefaults = false // Would need payment system integration
        };
    }

    /// <summary>
    /// Gathers salesperson historical data
    /// </summary>
    private async Task<SalespersonDiscountHistory?> GatherSalespersonHistoryAsync(
        Guid companyId,
        Guid salespersonId,
        CancellationToken cancellationToken)
    {
        var requests = await _discountRequestRepository.GetBySalespersonIdAsync(companyId, salespersonId, cancellationToken);
        var requestsList = requests.ToList();

        if (!requestsList.Any())
            return null;

        var approved = requestsList.Where(r => r.IsApproved()).ToList();
        var recent = requestsList.Where(r => r.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToList();
        var recentRejected = recent.Count(r => r.IsRejected());

        return new SalespersonDiscountHistory
        {
            TotalRequests = requestsList.Count,
            ApprovedRequests = approved.Count,
            AverageRequestedDiscount = requestsList.Average(r => r.RequestedDiscountPercentage),
            WinRate = 0.70m, // Would need sales outcome tracking
            RecentRejectionTrend = recent.Any() ? (decimal)recentRejected / recent.Count : 0m
        };
    }

    /// <summary>
    /// Validates the request
    /// </summary>
    private static void ValidateRequest(CalculateRiskScoreRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.CompanyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required", nameof(request));

        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required", nameof(request));

        if (request.SalespersonId == Guid.Empty)
            throw new ArgumentException("SalespersonId is required", nameof(request));

        if (request.RequestedDiscountPercentage < 0 || request.RequestedDiscountPercentage > 100)
            throw new ArgumentException("RequestedDiscountPercentage must be between 0 and 100", nameof(request));

        if (request.EstimatedMarginPercentage.HasValue &&
            (request.EstimatedMarginPercentage.Value < -100 || request.EstimatedMarginPercentage.Value > 100))
            throw new ArgumentException("EstimatedMarginPercentage must be between -100 and 100", nameof(request));
    }
}
