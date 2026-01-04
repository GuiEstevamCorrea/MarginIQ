using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Domain service for intelligent auto-approval of discount requests.
/// Auto-approval occurs when:
/// - Risk score is below threshold
/// - Request is within guardrails (business rules)
/// - AI model recommends with minimum confidence
/// </summary>
public class AutoApprovalService
{
    private readonly RiskScoreCalculationService _riskScoreService;
    private readonly BusinessRuleValidationService _businessRuleValidationService;

    // Default thresholds (can be overridden per company via business rules)
    private const decimal DefaultMaxRiskScoreForAutoApproval = 60m;
    private const decimal DefaultMinAIConfidenceForAutoApproval = 0.75m; // 75%
    private const decimal DefaultMaxDiscountForAutoApproval = 15m; // 15%

    public AutoApprovalService(
        RiskScoreCalculationService riskScoreService,
        BusinessRuleValidationService businessRuleValidationService)
    {
        _riskScoreService = riskScoreService ?? throw new ArgumentNullException(nameof(riskScoreService));
        _businessRuleValidationService = businessRuleValidationService ?? throw new ArgumentNullException(nameof(businessRuleValidationService));
    }

    /// <summary>
    /// Evaluates whether a discount request can be auto-approved
    /// </summary>
    /// <param name="discountRequest">Discount request to evaluate</param>
    /// <param name="customer">Customer associated with the request</param>
    /// <param name="salesperson">Salesperson who created the request</param>
    /// <param name="applicableRules">Business rules applicable to this request</param>
    /// <param name="riskScore">Pre-calculated risk score</param>
    /// <param name="aiConfidence">AI confidence level (0-1) - optional</param>
    /// <param name="productCosts">Dictionary of product costs</param>
    /// <returns>Auto-approval evaluation result</returns>
    public AutoApprovalEvaluation EvaluateAutoApproval(
        DiscountRequest discountRequest,
        Customer customer,
        User salesperson,
        IEnumerable<BusinessRule> applicableRules,
        decimal riskScore,
        decimal? aiConfidence = null,
        IDictionary<Guid, Money>? productCosts = null)
    {
        if (discountRequest == null)
            throw new ArgumentNullException(nameof(discountRequest));

        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        if (salesperson == null)
            throw new ArgumentNullException(nameof(salesperson));

        var evaluation = new AutoApprovalEvaluation
        {
            DiscountRequestId = discountRequest.Id,
            RiskScore = riskScore
        };

        var rules = applicableRules?.ToList() ?? new List<BusinessRule>();

        // Step 1: Check if AI is enabled for this company
        // (In production, this would come from Company settings)
        var aiEnabled = true; // Default to true for MVP

        // Step 2: Get auto-approval thresholds from business rules
        var thresholds = GetAutoApprovalThresholds(rules);
        evaluation.MaxRiskScoreThreshold = thresholds.MaxRiskScore.GetValueOrDefault(DefaultMaxRiskScoreForAutoApproval);
        evaluation.MinAIConfidenceThreshold = thresholds.MinAIConfidence.GetValueOrDefault(DefaultMinAIConfidenceForAutoApproval);

        // Step 3: Validate guardrails (business rules)
        var guardrailsValidation = ValidateGuardrails(
            discountRequest,
            customer,
            salesperson,
            rules,
            productCosts);

        evaluation.GuardrailsValidation = guardrailsValidation;

        if (!guardrailsValidation.IsValid)
        {
            evaluation.CanAutoApprove = false;
            evaluation.RejectionReason = "Request violates business rules (guardrails)";
            evaluation.RejectionDetails = guardrailsValidation.Errors;
            return evaluation;
        }

        // Step 4: Check risk score threshold
        var maxRiskScore = thresholds.MaxRiskScore.GetValueOrDefault(DefaultMaxRiskScoreForAutoApproval);
        if (riskScore > maxRiskScore)
        {
            evaluation.CanAutoApprove = false;
            evaluation.RejectionReason = $"Risk score ({riskScore:N2}) exceeds auto-approval threshold ({maxRiskScore:N2})";
            evaluation.RequiresHumanReview = true;
            return evaluation;
        }

        // Step 5: Check AI confidence (if AI recommendation is available)
        if (aiEnabled && aiConfidence.HasValue)
        {
            evaluation.AIConfidence = aiConfidence.Value;

            var minAIConfidence = thresholds.MinAIConfidence.GetValueOrDefault(DefaultMinAIConfidenceForAutoApproval);
            if (aiConfidence.Value < minAIConfidence)
            {
                evaluation.CanAutoApprove = false;
                evaluation.RejectionReason = $"AI confidence ({aiConfidence.Value:P0}) is below minimum threshold ({minAIConfidence:P0})";
                evaluation.RequiresHumanReview = true;
                return evaluation;
            }
        }
        else if (aiEnabled && !aiConfidence.HasValue)
        {
            // AI is enabled but no confidence provided - require human review
            evaluation.CanAutoApprove = false;
            evaluation.RejectionReason = "AI recommendation not available";
            evaluation.RequiresHumanReview = true;
            return evaluation;
        }

        // Step 6: Apply additional safety checks
        var safetyChecks = ApplySafetyChecks(discountRequest, customer, salesperson);
        evaluation.SafetyChecks = safetyChecks;

        if (!safetyChecks.Passed)
        {
            evaluation.CanAutoApprove = false;
            evaluation.RejectionReason = safetyChecks.FailureReason;
            evaluation.RequiresHumanReview = true;
            return evaluation;
        }

        // All checks passed - auto-approval is allowed
        evaluation.CanAutoApprove = true;
        evaluation.ApprovalReason = "All auto-approval criteria met: risk score within limits, guardrails validated, and AI confidence sufficient";

        return evaluation;
    }

    /// <summary>
    /// Validates guardrails (business rules) for auto-approval
    /// </summary>
    private ValidationResult ValidateGuardrails(
        DiscountRequest discountRequest,
        Customer customer,
        User salesperson,
        IEnumerable<BusinessRule> applicableRules,
        IDictionary<Guid, Money>? productCosts)
    {
        // Use existing business rule validation service
        return _businessRuleValidationService.ValidateDiscountRequest(
            discountRequest,
            customer,
            salesperson,
            applicableRules,
            productCosts ?? new Dictionary<Guid, Money>());
    }

    /// <summary>
    /// Gets auto-approval thresholds from business rules or uses defaults
    /// </summary>
    private AutoApprovalThresholds GetAutoApprovalThresholds(IEnumerable<BusinessRule> rules)
    {
        var thresholds = new AutoApprovalThresholds
        {
            MaxRiskScore = DefaultMaxRiskScoreForAutoApproval,
            MinAIConfidence = DefaultMinAIConfidenceForAutoApproval,
            MaxDiscountPercentage = DefaultMaxDiscountForAutoApproval
        };

        if (rules == null || !rules.Any())
            return thresholds;

        // Find auto-approval rules
        var autoApprovalRules = rules
            .Where(r => r.IsActive && r.IsAutoApprovalRule())
            .OrderBy(r => r.Priority)
            .ToList();

        foreach (var rule in autoApprovalRules)
        {
            var criteria = ParseAutoApprovalThresholds(rule);
            if (criteria == null)
                continue;

            // Use the most restrictive thresholds
            if (criteria.MaxRiskScore.HasValue && criteria.MaxRiskScore.Value < thresholds.MaxRiskScore.GetValueOrDefault(DefaultMaxRiskScoreForAutoApproval))
                thresholds.MaxRiskScore = criteria.MaxRiskScore.Value;

            if (criteria.MinAIConfidence.HasValue && criteria.MinAIConfidence.Value > thresholds.MinAIConfidence.GetValueOrDefault(DefaultMinAIConfidenceForAutoApproval))
                thresholds.MinAIConfidence = criteria.MinAIConfidence.Value;

            if (criteria.MaxDiscountPercentage.HasValue && criteria.MaxDiscountPercentage.Value < thresholds.MaxDiscountPercentage.GetValueOrDefault(DefaultMaxDiscountForAutoApproval))
                thresholds.MaxDiscountPercentage = criteria.MaxDiscountPercentage.Value;
        }

        return thresholds;
    }

    /// <summary>
    /// Applies additional safety checks before auto-approval
    /// </summary>
    private SafetyCheckResult ApplySafetyChecks(
        DiscountRequest discountRequest,
        Customer customer,
        User salesperson)
    {
        var result = new SafetyCheckResult { Passed = true };

        // Check 1: Customer must be active
        if (!customer.IsActive())
        {
            result.Passed = false;
            result.FailureReason = "Customer is not active";
            return result;
        }

        // Check 2: Salesperson must be active
        if (!salesperson.IsActive())
        {
            result.Passed = false;
            result.FailureReason = "Salesperson is not active";
            return result;
        }

        // Check 3: Request must not be too large (total value check)
        // This would require order value calculation
        var totalBasePrice = discountRequest.GetTotalBasePrice();
        var totalFinalPrice = discountRequest.GetTotalFinalPrice();

        // Safety check: prevent auto-approval of very large orders (> $100k equivalent)
        // In production, this threshold would be configurable per company
        if (totalBasePrice.Value > 100000m)
        {
            result.Passed = false;
            result.FailureReason = $"Order value ({totalBasePrice.Value:C}) exceeds auto-approval limit";
            return result;
        }

        // Check 4: No negative margins
        if (discountRequest.EstimatedMarginPercentage.HasValue && 
            discountRequest.EstimatedMarginPercentage.Value < 0m)
        {
            result.Passed = false;
            result.FailureReason = "Negative margin detected";
            return result;
        }

        // Check 5: Maximum items in request
        if (discountRequest.Items.Count > 50)
        {
            result.Passed = false;
            result.FailureReason = "Too many items in request for auto-approval";
            return result;
        }

        return result;
    }

    /// <summary>
    /// Checks if auto-approval feature is enabled for a company
    /// </summary>
    /// <param name="company">Company to check</param>
    /// <returns>True if auto-approval is enabled</returns>
    public bool IsAutoApprovalEnabled(Company company)
    {
        if (company == null)
            throw new ArgumentNullException(nameof(company));

        if (!company.IsActive())
            return false;

        // In production, this would parse company.GeneralSettings JSON
        // For now, return true for active companies
        return true;
    }

    /// <summary>
    /// Gets auto-approval statistics for a company
    /// </summary>
    /// <param name="totalAutoApproved">Number of auto-approved requests</param>
    /// <param name="totalRequests">Total number of requests</param>
    /// <returns>Auto-approval statistics</returns>
    public AutoApprovalStatistics GetAutoApprovalStatistics(
        int totalAutoApproved,
        int totalRequests)
    {
        var stats = new AutoApprovalStatistics
        {
            TotalAutoApproved = totalAutoApproved,
            TotalRequests = totalRequests
        };

        if (totalRequests > 0)
        {
            stats.AutoApprovalRate = (decimal)totalAutoApproved / totalRequests;
        }

        return stats;
    }

    /// <summary>
    /// Validates if a request can be manually overridden after auto-rejection
    /// </summary>
    /// <param name="user">User requesting override</param>
    /// <param name="evaluation">Auto-approval evaluation result</param>
    /// <returns>True if override is allowed</returns>
    public bool CanOverrideAutoRejection(User user, AutoApprovalEvaluation evaluation)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (evaluation == null)
            throw new ArgumentNullException(nameof(evaluation));

        // Only managers and admins can override auto-rejection
        if (!user.IsManager() && !user.IsAdmin())
            return false;

        // Cannot override if there are guardrail violations (hard rules)
        if (evaluation.GuardrailsValidation != null && !evaluation.GuardrailsValidation.IsValid)
            return false;

        return true;
    }

    // Helper method to parse auto-approval thresholds from business rule
    private AutoApprovalThresholds? ParseAutoApprovalThresholds(BusinessRule rule)
    {
        try
        {
            var parameters = rule.Parameters;
            var thresholds = new AutoApprovalThresholds();

            var maxRiskMatch = System.Text.RegularExpressions.Regex.Match(
                parameters, @"""maxRiskScore""\s*:\s*([\d.]+)");
            if (maxRiskMatch.Success && decimal.TryParse(maxRiskMatch.Groups[1].Value, out var maxRisk))
            {
                thresholds.MaxRiskScore = maxRisk;
            }

            var minConfidenceMatch = System.Text.RegularExpressions.Regex.Match(
                parameters, @"""minAIConfidence""\s*:\s*([\d.]+)");
            if (minConfidenceMatch.Success && decimal.TryParse(minConfidenceMatch.Groups[1].Value, out var minConfidence))
            {
                thresholds.MinAIConfidence = minConfidence;
            }

            var maxDiscountMatch = System.Text.RegularExpressions.Regex.Match(
                parameters, @"""maxDiscountPercentage""\s*:\s*([\d.]+)");
            if (maxDiscountMatch.Success && decimal.TryParse(maxDiscountMatch.Groups[1].Value, out var maxDiscount))
            {
                thresholds.MaxDiscountPercentage = maxDiscount;
            }

            return thresholds;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Auto-approval thresholds configuration
/// </summary>
public class AutoApprovalThresholds
{
    public decimal? MaxRiskScore { get; set; } = 60m;
    public decimal? MinAIConfidence { get; set; } = 0.75m;
    public decimal? MaxDiscountPercentage { get; set; } = 15m;
}

/// <summary>
/// Auto-approval evaluation result
/// </summary>
public class AutoApprovalEvaluation
{
    public Guid DiscountRequestId { get; set; }
    public bool CanAutoApprove { get; set; }
    public decimal RiskScore { get; set; }
    public decimal? AIConfidence { get; set; }
    public decimal MaxRiskScoreThreshold { get; set; }
    public decimal MinAIConfidenceThreshold { get; set; }
    public ValidationResult? GuardrailsValidation { get; set; }
    public SafetyCheckResult? SafetyChecks { get; set; }
    public string? ApprovalReason { get; set; }
    public string? RejectionReason { get; set; }
    public List<string> RejectionDetails { get; set; } = new();
    public bool RequiresHumanReview { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a summary explanation of the evaluation
    /// </summary>
    public string GetSummary()
    {
        if (CanAutoApprove)
        {
            return $"Auto-approval granted: {ApprovalReason}";
        }

        var summary = $"Auto-approval denied: {RejectionReason}";
        if (RejectionDetails.Any())
        {
            summary += "\nDetails: " + string.Join("; ", RejectionDetails);
        }

        return summary;
    }
}

/// <summary>
/// Safety check result
/// </summary>
public class SafetyCheckResult
{
    public bool Passed { get; set; }
    public string? FailureReason { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Auto-approval statistics
/// </summary>
public class AutoApprovalStatistics
{
    public int TotalAutoApproved { get; set; }
    public int TotalRequests { get; set; }
    public decimal AutoApprovalRate { get; set; }

    public string GetFormattedRate() => $"{AutoApprovalRate:P1}";
}
