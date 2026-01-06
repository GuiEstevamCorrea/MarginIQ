using Application.DTOs;
using Application.Ports;
using Domain.Repositories;
using Domain.Services;
using Domain.Entities;
using System.Diagnostics;

namespace Application.UseCases;

/// <summary>
/// Use Case: IA-UC-01 - Recommend Discount
/// 
/// Input:
/// - Customer
/// - Product(s)
/// - History
/// - Rules
/// 
/// Output:
/// - Recommended discount percentage
/// - Expected margin
/// - Confidence
/// 
/// This use case orchestrates the discount recommendation process,
/// using AI when available with fallback to rule-based recommendations.
/// </summary>
public class RecommendDiscountUseCase
{
    private readonly IAIService _aiService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBusinessRuleRepository _businessRuleRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly MarginCalculationService _marginCalculationService;
    private readonly BusinessRuleValidationService _businessRuleValidationService;

    public RecommendDiscountUseCase(
        IAIService aiService,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IBusinessRuleRepository businessRuleRepository,
        IDiscountRequestRepository discountRequestRepository,
        ICompanyRepository companyRepository,
        MarginCalculationService marginCalculationService,
        BusinessRuleValidationService businessRuleValidationService)
    {
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _businessRuleRepository = businessRuleRepository ?? throw new ArgumentNullException(nameof(businessRuleRepository));
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _companyRepository = companyRepository ?? throw new ArgumentNullException(nameof(companyRepository));
        _marginCalculationService = marginCalculationService ?? throw new ArgumentNullException(nameof(marginCalculationService));
        _businessRuleValidationService = businessRuleValidationService ?? throw new ArgumentNullException(nameof(businessRuleValidationService));
    }

    /// <summary>
    /// Executes the discount recommendation use case
    /// </summary>
    public async Task<RecommendDiscountResponse> ExecuteAsync(
        RecommendDiscountRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Validate input
        ValidateRequest(request);

        // Step 2: Load entities
        var company = await _companyRepository.GetByIdAsync(request.CompanyId)
            ?? throw new InvalidOperationException($"Company {request.CompanyId} not found");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new InvalidOperationException($"Customer {request.CustomerId} not found");

        var salesperson = await _userRepository.GetByIdAsync(request.SalespersonId)
            ?? throw new InvalidOperationException($"Salesperson {request.SalespersonId} not found");

        // Load products
        var products = new List<Product>();
        foreach (var item in request.Products)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                throw new InvalidOperationException($"Product {item.ProductId} not found");
            products.Add(product);
        }

        // Step 3: Gather context
        var customerHistory = request.IncludeCustomerHistory
            ? await GatherCustomerHistoryAsync(request.CompanyId, request.CustomerId, cancellationToken)
            : null;

        var businessRules = request.IncludeBusinessRules
            ? await _businessRuleRepository.GetActiveByCompanyIdAsync(request.CompanyId)
            : Array.Empty<BusinessRule>();

        // Step 4: Attempt AI recommendation if enabled
        RecommendDiscountResponse response;
        
        if (request.UseAI && await _aiService.IsAvailableAsync(request.CompanyId, cancellationToken))
        {
            try
            {
                response = await GetAIRecommendationAsync(
                    request,
                    customer,
                    salesperson,
                    products,
                    customerHistory,
                    businessRules,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // AI failed - fallback to rule-based
                response = GetRuleBasedRecommendation(
                    request,
                    customer,
                    salesperson,
                    products,
                    customerHistory,
                    businessRules);
                
                response.IsFallback = true;
                response.Source = "RuleBased";
                response.Recommendations.Add($"AI service unavailable: {ex.Message}. Using rule-based recommendation.");
            }
        }
        else
        {
            // Use rule-based recommendation
            response = GetRuleBasedRecommendation(
                request,
                customer,
                salesperson,
                products,
                customerHistory,
                businessRules);
            
            response.IsFallback = true;
            response.Source = "RuleBased";
        }

        // Step 5: Apply guardrails validation
        response.IsWithinGuardrails = ValidateGuardrails(
            response.RecommendedDiscountPercentage,
            salesperson,
            businessRules,
            response);

        stopwatch.Stop();
        response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        return response;
    }

    /// <summary>
    /// Gets AI-based recommendation
    /// </summary>
    private async Task<RecommendDiscountResponse> GetAIRecommendationAsync(
        RecommendDiscountRequest request,
        Customer customer,
        User salesperson,
        List<Product> products,
        CustomerDiscountHistory? customerHistory,
        IEnumerable<BusinessRule> businessRules,
        CancellationToken cancellationToken)
    {
        // Build AI request
        var aiRequest = new DiscountRecommendationRequest
        {
            CompanyId = request.CompanyId,
            CustomerId = request.CustomerId,
            SalespersonId = request.SalespersonId,
            Items = request.Products.Select((item, index) => new RecommendationItem
            {
                ProductId = item.ProductId,
                ProductName = products[index].Name,
                ProductCategory = products[index].Category ?? string.Empty,
                Quantity = item.Quantity,
                BasePrice = products[index].BasePrice.Value,
                Currency = products[index].BasePrice.Currency
            }).ToList(),
            CustomerHistory = customerHistory != null ? new CustomerHistoryData
            {
                TotalOrders = customerHistory.TotalRequests,
                AverageDiscount = customerHistory.AverageApprovedDiscount,
                MaxDiscountReceived = customerHistory.MaxApprovedDiscount,
                RejectedRequests = customerHistory.RejectedRequests,
                Classification = customer.GetClassificationTier(),
                HasPaymentIssues = customerHistory.HasPaymentDelays || customerHistory.HasDefaults
            } : null,
            BusinessRules = businessRules.Select(r => new BusinessRuleData
            {
                RuleType = r.Type.ToString(),
                Scope = r.Scope.ToString(),
                Parameters = r.Parameters
            }).ToList()
        };

        // Call AI service
        var aiRecommendation = await _aiService.RecommendDiscountAsync(aiRequest, cancellationToken);

        // Build response
        var response = new RecommendDiscountResponse
        {
            RecommendedDiscountPercentage = aiRecommendation.RecommendedDiscountPercentage,
            ExpectedMarginPercentage = aiRecommendation.ExpectedMarginPercentage,
            Confidence = aiRecommendation.Confidence,
            Explanation = aiRecommendation.Explanation,
            IsFallback = aiRecommendation.IsFallback,
            Source = aiRecommendation.IsFallback ? "RuleBased" : "AI",
            HistoricalAverageDiscount = customerHistory?.AverageApprovedDiscount
        };

        // Add context from business rules
        ApplyBusinessRuleContext(response, salesperson, businessRules);

        return response;
    }

    /// <summary>
    /// Gets rule-based recommendation (fallback when AI is unavailable)
    /// </summary>
    private RecommendDiscountResponse GetRuleBasedRecommendation(
        RecommendDiscountRequest request,
        Customer customer,
        User salesperson,
        List<Product> products,
        CustomerDiscountHistory? customerHistory,
        IEnumerable<BusinessRule> businessRules)
    {
        var response = new RecommendDiscountResponse
        {
            Confidence = 0.5m, // Medium confidence for rule-based
            Source = "RuleBased",
            IsFallback = true,
            Explanation = "Recommendation based on business rules and historical data"
        };

        // Strategy 1: Use customer's historical average if available
        if (customerHistory != null && customerHistory.TotalRequests > 0)
        {
            response.RecommendedDiscountPercentage = customerHistory.AverageApprovedDiscount;
            response.HistoricalAverageDiscount = customerHistory.AverageApprovedDiscount;
            response.Explanation = $"Based on customer's historical average discount of {customerHistory.AverageApprovedDiscount:N1}%";
            response.Confidence = 0.7m; // Higher confidence when we have history
        }
        else
        {
            // Strategy 2: Use conservative discount based on customer classification
            response.RecommendedDiscountPercentage = customer.Classification switch
            {
                Domain.Enums.CustomerClassification.A => 10m, // Top customers get standard 10%
                Domain.Enums.CustomerClassification.B => 7m,  // Mid-tier gets 7%
                Domain.Enums.CustomerClassification.C => 5m,  // Lower tier gets 5%
                _ => 3m // Unclassified or new customers get minimal discount
            };
            response.Explanation = $"Conservative recommendation based on customer classification ({customer.Classification})";
        }

        // Apply business rule constraints
        ApplyBusinessRuleContext(response, salesperson, businessRules);

        // Ensure recommendation is within allowed limits
        if (response.MaximumDiscount.HasValue && 
            response.RecommendedDiscountPercentage > response.MaximumDiscount.Value)
        {
            response.RecommendedDiscountPercentage = response.MaximumDiscount.Value;
            response.Recommendations.Add($"Discount capped at maximum allowed ({response.MaximumDiscount.Value:N1}%)");
        }

        // Estimate margin (simplified - would need product costs for accuracy)
        response.ExpectedMarginPercentage = EstimateMargin(products, response.RecommendedDiscountPercentage);

        return response;
    }

    /// <summary>
    /// Applies business rule context to the response
    /// </summary>
    private void ApplyBusinessRuleContext(
        RecommendDiscountResponse response,
        User salesperson,
        IEnumerable<BusinessRule> businessRules)
    {
        var rulesList = businessRules.ToList();

        // Find discount limit rules
        var discountLimitRules = rulesList
            .Where(r => r.IsActive && r.IsDiscountLimitRule())
            .ToList();

        decimal? maxDiscount = null;
        foreach (var rule in discountLimitRules)
        {
            if (rule.IsUserRoleScope() && rule.TargetIdentifier == salesperson.Role.ToString())
            {
                var limit = ParseMaxDiscountFromRule(rule);
                if (limit.HasValue)
                {
                    if (!maxDiscount.HasValue || limit.Value < maxDiscount.Value)
                        maxDiscount = limit.Value;
                }
            }
        }

        if (maxDiscount.HasValue)
        {
            response.MaximumDiscount = maxDiscount.Value;
            if (response.RecommendedDiscountPercentage > maxDiscount.Value)
            {
                response.RiskIndicators.Add($"Recommended discount exceeds maximum allowed for {salesperson.Role} role");
            }
        }

        // Find minimum margin rules
        var marginRules = rulesList
            .Where(r => r.IsActive && r.IsMinimumMarginRule())
            .ToList();

        if (marginRules.Any())
        {
            response.Recommendations.Add("Ensure resulting margin meets minimum requirements");
        }
    }

    /// <summary>
    /// Validates if recommendation is within guardrails
    /// </summary>
    private bool ValidateGuardrails(
        decimal recommendedDiscount,
        User salesperson,
        IEnumerable<BusinessRule> businessRules,
        RecommendDiscountResponse response)
    {
        // Check discount limits
        if (response.MaximumDiscount.HasValue && recommendedDiscount > response.MaximumDiscount.Value)
        {
            response.RiskIndicators.Add("Exceeds maximum allowed discount");
            return false;
        }

        // Check minimum margin
        if (response.ExpectedMarginPercentage < 0)
        {
            response.RiskIndicators.Add("Results in negative margin");
            return false;
        }

        return true;
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
            HasPaymentDelays = false, // Would need payment data
            HasDefaults = false // Would need payment data
        };
    }

    /// <summary>
    /// Estimates margin based on products and discount
    /// </summary>
    private decimal EstimateMargin(List<Product> products, decimal discountPercentage)
    {
        if (!products.Any())
            return 0m;

        // Use average base margin and adjust for discount
        var avgBaseMargin = products.Average(p => p.BaseMarginPercentage);
        
        // Simplified estimation: each 1% discount reduces margin by approximately 1.5%
        var estimatedMargin = avgBaseMargin - (discountPercentage * 1.5m);
        
        return Math.Max(0m, estimatedMargin);
    }

    /// <summary>
    /// Parses max discount from business rule
    /// </summary>
    private decimal? ParseMaxDiscountFromRule(BusinessRule rule)
    {
        try
        {
            var parameters = rule.Parameters;
            var match = System.Text.RegularExpressions.Regex.Match(
                parameters, @"""maxDiscountPercentage""\s*:\s*([\d.]+)");
            
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var value))
            {
                return value;
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return null;
    }

    /// <summary>
    /// Validates the request
    /// </summary>
    private static void ValidateRequest(RecommendDiscountRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.CompanyId == Guid.Empty)
            throw new ArgumentException("CompanyId is required", nameof(request));

        if (request.CustomerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required", nameof(request));

        if (request.SalespersonId == Guid.Empty)
            throw new ArgumentException("SalespersonId is required", nameof(request));

        if (request.Products == null || !request.Products.Any())
            throw new ArgumentException("At least one product is required", nameof(request));

        foreach (var product in request.Products)
        {
            if (product.ProductId == Guid.Empty)
                throw new ArgumentException("Product ID is required for all items");

            if (product.Quantity <= 0)
                throw new ArgumentException("Product quantity must be greater than zero");
        }
    }
}
