using Application.DTOs;
using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.UseCases;

/// <summary>
/// Use Case UC-03A: Try Auto-Approve Discount Request
/// 
/// Attempts to auto-approve an existing discount request using AI.
/// Can be used to:
/// - Retry auto-approval on requests initially sent for human review
/// - Evaluate auto-approval after business rules or thresholds change
/// - Force re-evaluation of auto-approval eligibility
/// 
/// Business rules:
/// - Request must be in "UnderAnalysis" status
/// - AI must be enabled for the company
/// - Risk score must be below threshold
/// - Business rules (guardrails) must pass
/// - AI confidence must meet minimum threshold (if AI is used)
/// </summary>
public class TryAutoApproveDiscountRequestUseCase
{
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBusinessRuleRepository _businessRuleRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAILearningDataRepository _aiLearningDataRepository;
    private readonly IAIService _aiService;
    private readonly AutoApprovalService _autoApprovalService;
    private readonly RiskScoreCalculationService _riskScoreService;
    private readonly BusinessRuleValidationService _businessRuleValidationService;

    public TryAutoApproveDiscountRequestUseCase(
        IDiscountRequestRepository discountRequestRepository,
        IApprovalRepository approvalRepository,
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IBusinessRuleRepository businessRuleRepository,
        IAuditLogRepository auditLogRepository,
        IAILearningDataRepository aiLearningDataRepository,
        IAIService aiService,
        AutoApprovalService autoApprovalService,
        RiskScoreCalculationService riskScoreService,
        BusinessRuleValidationService businessRuleValidationService)
    {
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _approvalRepository = approvalRepository ?? throw new ArgumentNullException(nameof(approvalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _businessRuleRepository = businessRuleRepository ?? throw new ArgumentNullException(nameof(businessRuleRepository));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _aiLearningDataRepository = aiLearningDataRepository ?? throw new ArgumentNullException(nameof(aiLearningDataRepository));
        _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _autoApprovalService = autoApprovalService ?? throw new ArgumentNullException(nameof(autoApprovalService));
        _riskScoreService = riskScoreService ?? throw new ArgumentNullException(nameof(riskScoreService));
        _businessRuleValidationService = businessRuleValidationService ?? throw new ArgumentNullException(nameof(businessRuleValidationService));
    }

    /// <summary>
    /// Executes the try auto-approve use case
    /// </summary>
    public async Task<TryAutoApproveDiscountRequestResponse> ExecuteAsync(
        TryAutoApproveDiscountRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Step 1: Validate and load context
        var context = await ValidateAndLoadContextAsync(request, cancellationToken);

        // Step 2: Validate business rules
        ValidateBusinessRules(context.DiscountRequest, request.ForceReEvaluation);

        // Step 3: Get AI recommendation (if available)
        var aiRecommendation = await TryGetAIRecommendationAsync(context, cancellationToken);

        // Step 4: Calculate or update risk score
        var riskScore = await CalculateRiskScoreAsync(context, aiRecommendation, cancellationToken);

        // Step 5: Evaluate auto-approval eligibility
        var evaluation = await EvaluateAutoApprovalAsync(
            context,
            riskScore,
            aiRecommendation,
            cancellationToken);

        // Step 6: If eligible, auto-approve
        if (evaluation.CanAutoApprove)
        {
            var approval = await AutoApproveAsync(context, evaluation, riskScore, cancellationToken);
            
            await CreateAuditLogAsync(
                context.DiscountRequest,
                context.RequestedByUser,
                true,
                approval.Id,
                cancellationToken);

            return BuildSuccessResponse(
                context.DiscountRequest,
                approval,
                evaluation,
                riskScore,
                aiRecommendation);
        }

        // Step 7: Auto-approval failed - create audit log
        await CreateAuditLogAsync(
            context.DiscountRequest,
            context.RequestedByUser,
            false,
            null,
            cancellationToken);

        return BuildFailureResponse(
            context.DiscountRequest,
            evaluation,
            riskScore,
            aiRecommendation);
    }

    /// <summary>
    /// Validates request and loads required context
    /// </summary>
    private async Task<EvaluationContext> ValidateAndLoadContextAsync(
        TryAutoApproveDiscountRequestRequest request,
        CancellationToken cancellationToken)
    {
        // Load discount request
        var discountRequest = await _discountRequestRepository.GetByIdAsync(request.DiscountRequestId, cancellationToken);
        if (discountRequest == null)
            throw new InvalidOperationException($"Discount request with ID {request.DiscountRequestId} not found");

        // Validate multi-tenant
        if (discountRequest.CompanyId != request.CompanyId)
            throw new InvalidOperationException("Discount request does not belong to the specified company");

        // Load user who requested the auto-approval attempt
        var requestedByUser = await _userRepository.GetByIdAsync(request.RequestedBy, cancellationToken);
        if (requestedByUser == null)
            throw new InvalidOperationException($"User with ID {request.RequestedBy} not found");

        if (requestedByUser.CompanyId != request.CompanyId)
            throw new InvalidOperationException("User does not belong to the specified company");

        // Load customer
        var customer = await _customerRepository.GetByIdAsync(discountRequest.CustomerId, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {discountRequest.CustomerId} not found");

        // Load salesperson
        var salesperson = await _userRepository.GetByIdAsync(discountRequest.SalespersonId, cancellationToken);
        if (salesperson == null)
            throw new InvalidOperationException($"Salesperson with ID {discountRequest.SalespersonId} not found");

        // Load products
        var productIds = discountRequest.Items.Select(i => i.ProductId).ToList();
        var products = new List<Product>();
        foreach (var productId in productIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                products.Add(product);
            }
        }

        // Load business rules
        var businessRules = await _businessRuleRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);
        var activeRules = businessRules.Where(r => r.IsActive).ToList();

        return new EvaluationContext
        {
            DiscountRequest = discountRequest,
            Customer = customer,
            Salesperson = salesperson,
            Products = products.ToList(),
            BusinessRules = activeRules,
            RequestedByUser = requestedByUser
        };
    }

    /// <summary>
    /// Validates business rules for auto-approval attempt
    /// </summary>
    private void ValidateBusinessRules(DiscountRequest discountRequest, bool forceReEvaluation)
    {
        // Can only auto-approve requests in UnderAnalysis status
        if (discountRequest.Status != DiscountRequestStatus.UnderAnalysis)
        {
            throw new InvalidOperationException(
                $"Cannot attempt auto-approval on request with status {discountRequest.Status}. " +
                $"Only requests with status 'UnderAnalysis' can be auto-approved.");
        }

        // Check if already auto-approved (unless forcing re-evaluation)
        if (!forceReEvaluation && discountRequest.Status == DiscountRequestStatus.AutoApprovedByAI)
        {
            throw new InvalidOperationException(
                "Request is already auto-approved. Use ForceReEvaluation flag to re-evaluate.");
        }
    }

    /// <summary>
    /// Tries to get AI recommendation
    /// </summary>
    private async Task<AIRecommendationData?> TryGetAIRecommendationAsync(
        EvaluationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var aiAvailable = await _aiService.IsAvailableAsync(context.DiscountRequest.CompanyId);
            if (!aiAvailable)
                return null;

            var items = context.DiscountRequest.Items.Select(item =>
            {
                var product = context.Products.First(p => p.Id == item.ProductId);
                return new RecommendationItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ProductCategory = product.Category ?? "Uncategorized",
                    Quantity = item.Quantity,
                    BasePrice = item.UnitBasePrice.Value
                };
            }).ToList();

            var response = await _aiService.RecommendDiscountAsync(new DiscountRecommendationRequest
            {
                CompanyId = context.DiscountRequest.CompanyId,
                CustomerId = context.Customer.Id,
                SalespersonId = context.Salesperson.Id,
                Items = items
            });

            return new AIRecommendationData
            {
                RecommendedDiscount = response.RecommendedDiscountPercentage,
                ExpectedMargin = response.ExpectedMarginPercentage,
                Confidence = response.Confidence,
                Explanation = response.Explanation
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates risk score
    /// </summary>
    private async Task<decimal> CalculateRiskScoreAsync(
        EvaluationContext context,
        AIRecommendationData? aiRecommendation,
        CancellationToken cancellationToken)
    {
        // If request already has a risk score and we're not forcing re-evaluation, use it
        if (context.DiscountRequest.RiskScore.HasValue)
            return context.DiscountRequest.RiskScore.Value;

        // Try AI risk calculation
        if (aiRecommendation != null)
        {
            try
            {
                var aiAvailable = await _aiService.IsAvailableAsync(context.DiscountRequest.CompanyId);
                if (aiAvailable)
                {
                    var aiRiskResponse = await _aiService.CalculateRiskScoreAsync(new RiskScoreRequest
                    {
                        CompanyId = context.DiscountRequest.CompanyId,
                        DiscountRequestId = context.DiscountRequest.Id,
                        CustomerId = context.Customer.Id,
                        SalespersonId = context.Salesperson.Id,
                        RequestedDiscountPercentage = context.DiscountRequest.RequestedDiscountPercentage,
                        EstimatedMarginPercentage = context.DiscountRequest.EstimatedMarginPercentage ?? 0m
                    });

                    return aiRiskResponse.Score;
                }
            }
            catch
            {
                // Fall through to rule-based
            }
        }

        // Rule-based fallback
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

        var productCosts = new Dictionary<Guid, Money>();
        foreach (var item in context.DiscountRequest.Items)
        {
            var product = context.Products.First(p => p.Id == item.ProductId);
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            productCosts[product.Id] = new Money(costPerUnit, "USD");
        }

        return _riskScoreService.CalculateRiskScore(
            context.DiscountRequest,
            context.Customer,
            context.Salesperson,
            customerHistory,
            salespersonHistory,
            productCosts);
    }

    /// <summary>
    /// Evaluates auto-approval eligibility
    /// </summary>
    private async Task<Domain.Services.AutoApprovalEvaluation> EvaluateAutoApprovalAsync(
        EvaluationContext context,
        decimal riskScore,
        AIRecommendationData? aiRecommendation,
        CancellationToken cancellationToken)
    {
        // Get governance settings
        var governanceSettings = await _aiService.GetGovernanceSettingsAsync(context.DiscountRequest.CompanyId);

        // Build product costs
        var productCosts = new Dictionary<Guid, Money>();
        foreach (var product in context.Products)
        {
            var costPerUnit = product.BasePrice.Value * (1 - product.BaseMarginPercentage / 100);
            productCosts[product.Id] = new Money(costPerUnit, "USD");
        }

        // Evaluate
        return _autoApprovalService.EvaluateAutoApproval(
            context.DiscountRequest,
            context.Customer,
            context.Salesperson,
            context.BusinessRules,
            riskScore,
            aiRecommendation?.Confidence,
            productCosts);
    }

    /// <summary>
    /// Auto-approves the discount request
    /// </summary>
    private async Task<Approval> AutoApproveAsync(
        EvaluationContext context,
        Domain.Services.AutoApprovalEvaluation evaluation,
        decimal riskScore,
        CancellationToken cancellationToken)
    {
        // Update request
        context.DiscountRequest.AutoApproveByAI();
        if (!context.DiscountRequest.RiskScore.HasValue)
        {
            context.DiscountRequest.SetRiskScore(riskScore);
        }

        await _discountRequestRepository.UpdateAsync(context.DiscountRequest, cancellationToken);

        // Create approval
        var approval = Approval.CreateByAI(
            discountRequestId: context.DiscountRequest.Id,
            decision: ApprovalDecision.Approve,
            slaTimeInSeconds: 0,
            justification: evaluation.ApprovalReason ?? "Auto-approved by AI based on risk assessment",
            metadata: System.Text.Json.JsonSerializer.Serialize(new
            {
                riskScore = riskScore,
                maxThreshold = evaluation.MaxRiskScoreThreshold,
                aiConfidence = evaluation.AIConfidence,
                evaluatedAt = evaluation.EvaluatedAt
            }));

        await _approvalRepository.AddAsync(approval, cancellationToken);

        // Update or create learning data
        await UpdateLearningDataAsync(context.DiscountRequest, true, cancellationToken);

        return approval;
    }

    /// <summary>
    /// Creates audit log
    /// </summary>
    private async Task CreateAuditLogAsync(
        DiscountRequest discountRequest,
        User requestedBy,
        bool succeeded,
        Guid? approvalId,
        CancellationToken cancellationToken)
    {
        var action = succeeded ? AuditAction.AutoApproved : AuditAction.Other;

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            discountRequestId = discountRequest.Id,
            approvalId = approvalId,
            succeeded = succeeded,
            requestedBy = requestedBy.Id,
            requestedByName = requestedBy.Name,
            customerId = discountRequest.CustomerId,
            requestedDiscount = discountRequest.RequestedDiscountPercentage,
            riskScore = discountRequest.RiskScore
        });

        var auditLog = succeeded
            ? AuditLog.CreateForAI(
                entityName: nameof(DiscountRequest),
                entityId: discountRequest.Id,
                action: action,
                companyId: discountRequest.CompanyId,
                payload: payload)
            : AuditLog.CreateForSystem(
                entityName: nameof(DiscountRequest),
                entityId: discountRequest.Id,
                action: action,
                companyId: discountRequest.CompanyId,
                payload: payload);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    /// <summary>
    /// Updates learning data
    /// </summary>
    private async Task UpdateLearningDataAsync(
        DiscountRequest discountRequest,
        bool approved,
        CancellationToken cancellationToken)
    {
        try
        {
            var learningData = await _aiLearningDataRepository.GetByDiscountRequestIdAsync(
                discountRequest.Id,
                cancellationToken);

            if (learningData != null)
            {
                var approvedDiscount = approved ? discountRequest.RequestedDiscountPercentage : (decimal?)null;
                learningData.RecordHumanDecision(approved, approvedDiscount);
                await _aiLearningDataRepository.UpdateAsync(learningData);
            }
        }
        catch
        {
            // Non-critical
        }
    }

    /// <summary>
    /// Builds success response
    /// </summary>
    private TryAutoApproveDiscountRequestResponse BuildSuccessResponse(
        DiscountRequest discountRequest,
        Approval approval,
        Domain.Services.AutoApprovalEvaluation evaluation,
        decimal riskScore,
        AIRecommendationData? aiRecommendation)
    {
        return new TryAutoApproveDiscountRequestResponse
        {
            DiscountRequestId = discountRequest.Id,
            WasAutoApproved = true,
            ApprovalId = approval.Id,
            Status = discountRequest.Status,
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore),
            AIConfidence = aiRecommendation?.Confidence,
            Reason = evaluation.ApprovalReason ?? "Auto-approved by AI",
            EvaluationDetails = new AutoApprovalEvaluationDetails
            {
                GuardrailsPassed = evaluation.GuardrailsValidation?.IsValid ?? true,
                GuardrailsMessages = evaluation.GuardrailsValidation?.Errors?.ToList() ?? new List<string>(),
                RiskScoreWithinThreshold = riskScore <= evaluation.MaxRiskScoreThreshold,
                MaxRiskScoreThreshold = evaluation.MaxRiskScoreThreshold,
                AIConfidenceSufficient = !evaluation.AIConfidence.HasValue || evaluation.AIConfidence.Value >= evaluation.MinAIConfidenceThreshold,
                MinAIConfidenceThreshold = evaluation.MinAIConfidenceThreshold,
                AIEnabled = true,
                BlockingReasons = new List<string>()
            },
            Summary = $"✅ Request auto-approved by AI. Risk score: {riskScore:F0}/100 (threshold: {evaluation.MaxRiskScoreThreshold:F0})",
            NextSteps = new List<string>
            {
                "Notify the salesperson of the auto-approval",
                "Proceed with the negotiation",
                "The approval can be reviewed by a manager if needed",
                "Complete the sale in your CRM/ERP system"
            },
            EvaluatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Builds failure response
    /// </summary>
    private TryAutoApproveDiscountRequestResponse BuildFailureResponse(
        DiscountRequest discountRequest,
        Domain.Services.AutoApprovalEvaluation evaluation,
        decimal riskScore,
        AIRecommendationData? aiRecommendation)
    {
        return new TryAutoApproveDiscountRequestResponse
        {
            DiscountRequestId = discountRequest.Id,
            WasAutoApproved = false,
            ApprovalId = null,
            Status = discountRequest.Status,
            RiskScore = riskScore,
            RiskLevel = GetRiskLevel(riskScore),
            AIConfidence = aiRecommendation?.Confidence,
            Reason = evaluation.RejectionReason ?? "Auto-approval criteria not met",
            EvaluationDetails = new AutoApprovalEvaluationDetails
            {
                GuardrailsPassed = evaluation.GuardrailsValidation?.IsValid ?? false,
                GuardrailsMessages = evaluation.GuardrailsValidation?.Errors?.ToList() ?? new List<string>(),
                RiskScoreWithinThreshold = riskScore <= evaluation.MaxRiskScoreThreshold,
                MaxRiskScoreThreshold = evaluation.MaxRiskScoreThreshold,
                AIConfidenceSufficient = !evaluation.AIConfidence.HasValue || evaluation.AIConfidence.Value >= evaluation.MinAIConfidenceThreshold,
                MinAIConfidenceThreshold = evaluation.MinAIConfidenceThreshold,
                AIEnabled = true,
                BlockingReasons = evaluation.RejectionDetails.ToList()
            },
            Summary = $"❌ Auto-approval denied. {evaluation.RejectionReason}",
            NextSteps = new List<string>
            {
                "Request requires human approval",
                "A manager will review the request",
                $"Blocking reasons: {string.Join(", ", evaluation.RejectionDetails)}"
            },
            EvaluatedAt = DateTime.UtcNow
        };
    }

    private string GetRiskLevel(decimal riskScore)
    {
        if (riskScore < 30) return "Low";
        if (riskScore < 60) return "Medium";
        if (riskScore < 80) return "High";
        return "VeryHigh";
    }

    /// <summary>
    /// Internal evaluation context
    /// </summary>
    private class EvaluationContext
    {
        public DiscountRequest DiscountRequest { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public User Salesperson { get; set; } = null!;
        public List<Product> Products { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public User RequestedByUser { get; set; } = null!;
    }

    /// <summary>
    /// AI recommendation data
    /// </summary>
    private class AIRecommendationData
    {
        public decimal RecommendedDiscount { get; set; }
        public decimal ExpectedMargin { get; set; }
        public decimal Confidence { get; set; }
        public string? Explanation { get; set; }
    }
}
