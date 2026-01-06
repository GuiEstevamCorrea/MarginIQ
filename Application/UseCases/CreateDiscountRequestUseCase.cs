using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.UseCases;

/// <summary>
/// Use Case: Create Discount Request (UC-01 – Criar Solicitação de Desconto)
/// Main workflow that orchestrates the entire discount request process:
/// 1. Validate user and company
/// 2. Calculate margin
/// 3. Apply business rules
/// 4. Call AI for recommendation and risk score
/// 5. Decide: auto-approve or send for human approval
/// </summary>
public class CreateDiscountRequestUseCase
{
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IBusinessRuleRepository _businessRuleRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAILearningDataRepository _learningDataRepository;
    private readonly MarginCalculationService _marginService;
    private readonly BusinessRuleValidationService _validationService;
    private readonly RiskScoreCalculationService _riskScoreService;
    private readonly AutoApprovalService _autoApprovalService;
    private readonly IAIService _aiService;

    public CreateDiscountRequestUseCase(
        IDiscountRequestRepository discountRequestRepository,
        ICustomerRepository customerRepository,
        IUserRepository userRepository,
        IProductRepository productRepository,
        ICompanyRepository companyRepository,
        IBusinessRuleRepository businessRuleRepository,
        IApprovalRepository approvalRepository,
        IAuditLogRepository auditLogRepository,
        IAILearningDataRepository learningDataRepository,
        MarginCalculationService marginService,
        BusinessRuleValidationService validationService,
        RiskScoreCalculationService riskScoreService,
        AutoApprovalService autoApprovalService,
        IAIService aiService)
    {
        _discountRequestRepository = discountRequestRepository;
        _customerRepository = customerRepository;
        _userRepository = userRepository;
        _productRepository = productRepository;
        _companyRepository = companyRepository;
        _businessRuleRepository = businessRuleRepository;
        _approvalRepository = approvalRepository;
        _auditLogRepository = auditLogRepository;
        _learningDataRepository = learningDataRepository;
        _marginService = marginService;
        _validationService = validationService;
        _riskScoreService = riskScoreService;
        _autoApprovalService = autoApprovalService;
        _aiService = aiService;
    }

    /// <summary>
    /// Executes the discount request creation workflow
    /// </summary>
    public async Task<CreateDiscountRequestResponse> ExecuteAsync(
        CreateDiscountRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate user and company
        var validationContext = await ValidateAndLoadContextAsync(request, cancellationToken);

        var response = new CreateDiscountRequestResponse();

        // Step 2: Get AI recommendation (if requested)
        AIRecommendationSummary? aiRecommendation = null;
        if (request.RequestAIRecommendation)
        {
            aiRecommendation = await GetAIRecommendationAsync(
                validationContext,
                request,
                cancellationToken);
            response.AIRecommendation = aiRecommendation;
        }

        // Step 3: Create discount request items
        var items = CreateDiscountRequestItems(
            request.Items,
            validationContext.Products,
            request.RequestedDiscountPercentage);

        // Step 4: Calculate estimated margin
        var estimatedMargin = CalculateEstimatedMargin(items, validationContext.Products);
        response.EstimatedMarginPercentage = estimatedMargin;

        // Step 5: Create the discount request entity
        var discountRequest = new DiscountRequest(
            customerId: request.CustomerId,
            salespersonId: request.SalespersonId,
            companyId: request.CompanyId,
            items: items,
            requestedDiscountPercentage: request.RequestedDiscountPercentage,
            comments: request.Comments);

        // Set estimated margin using domain method
        // Note: EstimatedMarginPercentage will be set during business rule validation

        // Step 6: Apply business rules validation
        var businessRulesResult = await ValidateBusinessRulesAsync(
            discountRequest,
            validationContext,
            cancellationToken);

        response.ValidationResult = businessRulesResult;

        // Check if request can proceed
        if (!businessRulesResult.CanProceed)
        {
            throw new InvalidOperationException(
                $"Discount request blocked by business rules: {businessRulesResult.BlockingReason}");
        }

        // Step 7: Calculate risk score (AI or rule-based)
        var riskScoreResult = await CalculateRiskScoreAsync(
            discountRequest,
            validationContext,
            request.UseAIForRiskScore,
            cancellationToken);

        discountRequest.SetRiskScore(riskScoreResult.RiskScore);
        response.RiskScore = riskScoreResult.RiskScore;
        response.RiskLevel = riskScoreResult.RiskLevel;

        // Add warnings from risk calculation
        response.Warnings.AddRange(riskScoreResult.RiskFactors);

        // Step 8: Save discount request
        await _discountRequestRepository.AddAsync(discountRequest, cancellationToken);

        response.DiscountRequestId = discountRequest.Id;
        response.Status = discountRequest.Status.ToString();
        response.CreatedAt = DateTime.UtcNow;

        // Step 9: Decide auto-approval or human approval
        if (request.AllowAutoApproval)
        {
            var autoApprovalResult = await TryAutoApprovalAsync(
                discountRequest,
                validationContext,
                riskScoreResult,
                aiRecommendation,
                cancellationToken);

            response.WasAutoApproved = autoApprovalResult.WasApproved;
            response.ApprovalId = autoApprovalResult.ApprovalId;

            if (autoApprovalResult.WasApproved)
            {
                response.Status = discountRequest.Status.ToString();
                response.NextSteps.Add("Discount request has been auto-approved by AI");
                response.NextSteps.Add("You can proceed with the sale");
            }
            else
            {
                response.NextSteps.Add($"Discount request requires human approval: {autoApprovalResult.Reason}");
                response.NextSteps.Add("Wait for manager decision");
            }
        }
        else
        {
            response.NextSteps.Add("Discount request requires human approval (auto-approval disabled)");
            response.NextSteps.Add("Wait for manager decision");
        }

        // Step 10: Create audit log
        await CreateAuditLogAsync(discountRequest, request.SalespersonId, cancellationToken);

        // Step 11: Add warnings from validation
        if (businessRulesResult.Warnings.Any())
        {
            response.Warnings.AddRange(businessRulesResult.Warnings);
        }

        return response;
    }

    /// <summary>
    /// Validates request and loads all required entities
    /// </summary>
    private async Task<ValidationContext> ValidateAndLoadContextAsync(
        CreateDiscountRequestRequest request,
        CancellationToken cancellationToken)
    {
        // Validate company
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company == null)
        {
            throw new InvalidOperationException($"Company {request.CompanyId} not found");
        }

        if (company.Status != CompanyStatus.Active)
        {
            throw new InvalidOperationException($"Company is not active (Status: {company.Status})");
        }

        // Validate salesperson
        var salesperson = await _userRepository.GetByIdAsync(request.SalespersonId, cancellationToken);
        if (salesperson == null)
        {
            throw new InvalidOperationException($"Salesperson {request.SalespersonId} not found");
        }

        if (salesperson.CompanyId != request.CompanyId)
        {
            throw new InvalidOperationException("Salesperson does not belong to the specified company");
        }

        if (salesperson.Status != UserStatus.Active)
        {
            throw new InvalidOperationException($"Salesperson is not active (Status: {salesperson.Status})");
        }

        // Validate customer
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
        {
            throw new InvalidOperationException($"Customer {request.CustomerId} not found");
        }

        if (customer.CompanyId != request.CompanyId)
        {
            throw new InvalidOperationException("Customer does not belong to the specified company");
        }

        if (customer.Status == CustomerStatus.Blocked)
        {
            throw new InvalidOperationException("Cannot create discount request for blocked customer");
        }

        // Validate and load products
        var products = new List<Product>();
        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
            {
                throw new InvalidOperationException($"Product {item.ProductId} not found");
            }

            if (product.CompanyId != request.CompanyId)
            {
                throw new InvalidOperationException($"Product {item.ProductId} does not belong to the specified company");
            }

            if (product.Status != ProductStatus.Active)
            {
                throw new InvalidOperationException($"Product {product.Name} is not active");
            }

            products.Add(product);
        }

        // Load active business rules
        var businessRules = (await _businessRuleRepository.GetByCompanyIdAsync(
            request.CompanyId,
            cancellationToken))
            .Where(r => r.IsActive)
            .ToList();

        return new ValidationContext
        {
            Company = company,
            Salesperson = salesperson,
            Customer = customer,
            Products = products,
            BusinessRules = businessRules
        };
    }

    /// <summary>
    /// Gets AI recommendation for the discount
    /// </summary>
    private async Task<AIRecommendationSummary?> GetAIRecommendationAsync(
        ValidationContext context,
        CreateDiscountRequestRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _aiService.IsAvailableAsync(request.CompanyId, cancellationToken))
            {
                return null;
            }

            // Prepare AI recommendation request
            var aiRequest = new DiscountRecommendationRequest
            {
                CompanyId = request.CompanyId,
                CustomerId = request.CustomerId,
                SalespersonId = request.SalespersonId,
                Items = request.Items.Select((item, index) =>
                {
                    var product = context.Products[index];
                    return new RecommendationItem
                    {
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        ProductCategory = product.Category ?? "Uncategorized",
                        Quantity = item.Quantity,
                        BasePrice = item.UnitPrice,
                        Currency = "USD" // Could be from company settings
                    };
                }).ToList()
            };

            var aiResponse = await _aiService.RecommendDiscountAsync(aiRequest, cancellationToken);

            return new AIRecommendationSummary
            {
                RecommendedDiscount = aiResponse.RecommendedDiscountPercentage,
                ExpectedMargin = aiResponse.ExpectedMarginPercentage,
                Confidence = aiResponse.Confidence,
                Source = aiResponse.IsFallback ? "RuleBased" : "AI",
                Explanation = aiResponse.IsFallback ? "Rule-based fallback" : "AI recommendation"
            };
        }
        catch (Exception)
        {
            // If AI fails, return null and continue with rule-based approach
            return null;
        }
    }

    /// <summary>
    /// Creates discount request items from DTOs
    /// </summary>
    private List<DiscountRequestItem> CreateDiscountRequestItems(
        List<DiscountRequestItemDTO> itemDTOs,
        List<Product> products,
        decimal overallDiscount)
    {
        var items = new List<DiscountRequestItem>();

        for (int i = 0; i < itemDTOs.Count; i++)
        {
            var dto = itemDTOs[i];
            var product = products[i];

            var unitPrice = new Money(dto.UnitPrice, "USD");
            var discountPercentage = dto.ItemDiscountPercentage ?? overallDiscount;

            var item = new DiscountRequestItem(
                productId: dto.ProductId,
                productName: product.Name,
                quantity: (int)dto.Quantity, // Convert to int
                unitBasePrice: unitPrice,
                discountPercentage: discountPercentage);

            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// Calculates estimated margin for the discount request
    /// </summary>
    private decimal CalculateEstimatedMargin(
        List<DiscountRequestItem> items,
        List<Product> products)
    {
        decimal totalRevenue = 0;
        decimal totalCost = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var product = products[i];

            var finalPrice = item.GetTotalFinalPrice();
            // Estimate cost from base margin percentage
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            var cost = costPerUnit * item.Quantity;

            totalRevenue += finalPrice.Value;
            totalCost += cost;
        }

        if (totalRevenue == 0) return 0;

        return _marginService.CalculateMarginPercentage(
            new Money(totalRevenue, "USD"),
            new Money(totalCost, "USD"));
    }

    /// <summary>
    /// Validates business rules for the discount request
    /// </summary>
    private Task<BusinessRulesValidationResult> ValidateBusinessRulesAsync(
        DiscountRequest discountRequest,
        ValidationContext context,
        CancellationToken cancellationToken)
    {
        var result = new BusinessRulesValidationResult
        {
            IsValid = true,
            CanProceed = true
        };

        // Build product costs dictionary
        var productCosts = new Dictionary<Guid, Money>();
        for (int i = 0; i < context.Products.Count; i++)
        {
            var product = context.Products[i];
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            productCosts[product.Id] = new Money(costPerUnit, "USD");
        }

        // Apply business rules validation
        var validationResult = _validationService.ValidateDiscountRequest(
            discountRequest,
            context.Customer,
            context.Salesperson,
            context.BusinessRules,
            productCosts);

        result.IsValid = validationResult.IsValid;
        result.ViolatedRules = validationResult.Errors.ToList();
        result.Warnings = validationResult.Warnings.ToList();

        // Check if any violations are blocking
        if (!validationResult.IsValid)
        {
            result.CanProceed = false;
            result.BlockingReason = string.Join("; ", validationResult.Errors);
        }

        // Map applied rules
        result.AppliedRules = context.BusinessRules.Select(rule => new AppliedBusinessRule
        {
            RuleId = rule.Id,
            RuleType = rule.Type.ToString(),
            Scope = rule.Scope.ToString(),
            Passed = validationResult.IsValid,
            Message = $"{rule.Type} - {rule.Scope}"
        }).ToList();

        return Task.FromResult(result);
    }

    /// <summary>
    /// Calculates risk score using AI or rule-based approach
    /// </summary>
    private async Task<RiskScoreResult> CalculateRiskScoreAsync(
        DiscountRequest discountRequest,
        ValidationContext context,
        bool useAI,
        CancellationToken cancellationToken)
    {
        // Try AI if enabled
        if (useAI && await _aiService.IsAvailableAsync(context.Company.Id, cancellationToken))
        {
            try
            {
                var aiRequest = new RiskScoreRequest
                {
                    CompanyId = context.Company.Id,
                    CustomerId = context.Customer.Id,
                    SalespersonId = context.Salesperson.Id,
                    RequestedDiscountPercentage = discountRequest.RequestedDiscountPercentage,
                    EstimatedMarginPercentage = discountRequest.EstimatedMarginPercentage ?? 0
                };

                var aiResponse = await _aiService.CalculateRiskScoreAsync(aiRequest, cancellationToken);

                return new RiskScoreResult
                {
                    RiskScore = aiResponse.Score,
                    RiskLevel = aiResponse.RiskLevel,
                    RiskFactors = aiResponse.RiskFactors,
                    Source = aiResponse.IsFallback ? "RuleBased" : "AI"
                };
            }
            catch
            {
                // Fall through to rule-based
            }
        }

        // Rule-based fallback - simplified version
        // In production, would gather full history
        var customerHistory = new CustomerDiscountHistory
        {
            TotalRequests = 0,
            ApprovedRequests = 0,
            RejectedRequests = 0,
            AverageApprovedDiscount = 0
        };

        var salespersonHistory = new SalespersonDiscountHistory
        {
            TotalRequests = 0,
            ApprovedRequests = 0,
            AverageRequestedDiscount = 0
        };

        // Build productCosts dictionary
        var productCosts = new Dictionary<Guid, Money>();
        foreach (var item in discountRequest.Items)
        {
            var product = context.Products.First(p => p.Id == item.ProductId);
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            productCosts[product.Id] = new Money(costPerUnit, "USD");
        }

        var riskScore = _riskScoreService.CalculateRiskScore(
            discountRequest,
            context.Customer,
            context.Salesperson,
            customerHistory,
            salespersonHistory,
            productCosts);

        return new RiskScoreResult
        {
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore),
            RiskFactors = new List<string> { $"Risk score: {riskScore:F0}/100" },
            Source = "RuleBased"
        };
    }

    /// <summary>
    /// Attempts auto-approval based on AI governance and risk assessment
    /// </summary>
    private async Task<AutoApprovalResult> TryAutoApprovalAsync(
        DiscountRequest discountRequest,
        ValidationContext context,
        RiskScoreResult riskScoreResult,
        AIRecommendationSummary? aiRecommendation,
        CancellationToken cancellationToken)
    {
        // Get AI governance settings
        var governanceSettings = await _aiService.GetGovernanceSettingsAsync(
            context.Company.Id,
            cancellationToken);

        // Check if AI is enabled for auto-approval
        if (!governanceSettings.AIEnabled || governanceSettings.RequireHumanReview)
        {
            return new AutoApprovalResult
            {
                WasApproved = false,
                Reason = "AI auto-approval is disabled for this company"
            };
        }

        // Build product costs for evaluation
        var productCosts = new Dictionary<Guid, Money>();
        foreach (var product in context.Products)
        {
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            productCosts[product.Id] = new Money(costPerUnit, "USD");
        }

        // Use auto-approval service to decide
        var evaluation = _autoApprovalService.EvaluateAutoApproval(
            discountRequest,
            context.Customer,
            context.Salesperson,
            context.BusinessRules,
            riskScoreResult.RiskScore,
            aiRecommendation?.Confidence,
            productCosts);

        if (!evaluation.CanAutoApprove)
        {
            return new AutoApprovalResult
            {
                WasApproved = false,
                Reason = string.Join("; ", evaluation.RejectionDetails)
            };
        }

        // Auto-approve!
        discountRequest.AutoApproveByAI();

        // Create AI approval record using the factory method
        var approval = Approval.CreateByAI(
            discountRequestId: discountRequest.Id,
            decision: ApprovalDecision.Approve,
            slaTimeInSeconds: 0, // Instant
            justification: "Auto-approved by AI",
            metadata: null);

        await _approvalRepository.AddAsync(approval, cancellationToken);
        await _discountRequestRepository.UpdateAsync(discountRequest, cancellationToken);

        // Create learning data for future training
        await CreateLearningDataAsync(discountRequest, approval, context, cancellationToken);

        return new AutoApprovalResult
        {
            WasApproved = true,
            ApprovalId = approval.Id,
            Reason = "Auto-approved by AI based on low risk and high confidence"
        };
    }

    /// <summary>
    /// Creates audit log for discount request creation
    /// </summary>
    private async Task CreateAuditLogAsync(
        DiscountRequest discountRequest,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.CreateForHuman(
            entityName: "DiscountRequest",
            entityId: discountRequest.Id,
            action: AuditAction.Created,
            companyId: discountRequest.CompanyId,
            userId: userId,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                CustomerId = discountRequest.CustomerId,
                SalespersonId = discountRequest.SalespersonId,
                RequestedDiscount = discountRequest.RequestedDiscountPercentage,
                EstimatedMargin = discountRequest.EstimatedMarginPercentage,
                RiskScore = discountRequest.RiskScore,
                Status = discountRequest.Status.ToString()
            }));

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Creates learning data for AI training
    /// </summary>
    private async Task CreateLearningDataAsync(
        DiscountRequest discountRequest,
        Approval approval,
        ValidationContext context,
        CancellationToken cancellationToken)
    {
        var productsJson = System.Text.Json.JsonSerializer.Serialize(
            discountRequest.Items.Select(i => new
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitBasePrice = i.UnitBasePrice.Value,
                Discount = i.DiscountPercentage
            }));

        var totalBasePrice = discountRequest.Items.Sum(i => i.GetTotalBasePrice().Value);
        var totalFinalPrice = discountRequest.Items.Sum(i => i.GetTotalFinalPrice().Value);

        var learningData = new AILearningData(
            companyId: discountRequest.CompanyId,
            discountRequestId: discountRequest.Id,
            customerId: context.Customer.Id,
            customerName: context.Customer.Name,
            customerSegment: context.Customer.Segment,
            customerClassification: context.Customer.Classification,
            salespersonId: context.Salesperson.Id,
            salespersonName: context.Salesperson.Name,
            salespersonRole: context.Salesperson.Role,
            productsJson: productsJson,
            requestedDiscountPercentage: discountRequest.RequestedDiscountPercentage,
            approvedDiscountPercentage: discountRequest.RequestedDiscountPercentage,
            baseMarginPercentage: 0, // Would need to calculate from products
            finalMarginPercentage: discountRequest.EstimatedMarginPercentage ?? 0,
            totalBasePrice: totalBasePrice,
            totalFinalPrice: totalFinalPrice,
            currency: "USD",
            decision: approval.Decision,
            decisionSource: approval.Source,
            riskScore: discountRequest.RiskScore ?? 0,
            aiConfidence: null,
            requestCreatedAt: DateTime.UtcNow,
            decisionMadeAt: approval.DecisionDateTime);

        await _learningDataRepository.AddAsync(learningData);
    }

    private string GetRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            < 30 => "Low",
            < 60 => "Medium",
            < 80 => "High",
            _ => "VeryHigh"
        };
    }

    private string GetAutoApprovalBlockReason(
        DiscountRequest request,
        RiskScoreResult riskScore,
        AIRecommendationSummary? aiRecommendation,
        AIGovernanceSettings settings)
    {
        var reasons = new List<string>();

        if (riskScore.RiskScore > settings.MaxRiskScoreForAutoApproval)
        {
            reasons.Add($"Risk score too high ({riskScore.RiskScore:F0} > {settings.MaxRiskScoreForAutoApproval:F0})");
        }

        if (aiRecommendation != null && aiRecommendation.Confidence < settings.MinConfidenceForAutoApproval)
        {
            reasons.Add($"AI confidence too low ({aiRecommendation.Confidence:P0} < {settings.MinConfidenceForAutoApproval:P0})");
        }

        if (request.RequestedDiscountPercentage > settings.MaxAutoApprovalDiscount)
        {
            reasons.Add($"Discount too high ({request.RequestedDiscountPercentage:F1}% > {settings.MaxAutoApprovalDiscount:F1}%)");
        }

        return reasons.Any() ? string.Join("; ", reasons) : "Does not meet auto-approval criteria";
    }
}

/// <summary>
/// Validation context with loaded entities
/// </summary>
internal class ValidationContext
{
    public Company Company { get; set; } = null!;
    public User Salesperson { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public List<Product> Products { get; set; } = new();
    public List<BusinessRule> BusinessRules { get; set; } = new();
}

/// <summary>
/// Risk score calculation result
/// </summary>
internal class RiskScoreResult
{
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Auto-approval attempt result
/// </summary>
internal class AutoApprovalResult
{
    public bool WasApproved { get; set; }
    public Guid? ApprovalId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
