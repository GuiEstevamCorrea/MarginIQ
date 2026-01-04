using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Domain service for validating business rules for discount requests.
/// Implements the business rules:
/// - Discount cannot exceed limit by user role/profile
/// - Margin cannot fall below configured minimum
/// - Blocked customer → automatic rejection
/// </summary>
public class BusinessRuleValidationService
{
    private readonly MarginCalculationService _marginCalculationService;

    public BusinessRuleValidationService(MarginCalculationService marginCalculationService)
    {
        _marginCalculationService = marginCalculationService ?? throw new ArgumentNullException(nameof(marginCalculationService));
    }

    /// <summary>
    /// Validates all business rules for a discount request
    /// </summary>
    /// <param name="discountRequest">Discount request to validate</param>
    /// <param name="customer">Customer associated with the request</param>
    /// <param name="salesperson">Salesperson who created the request</param>
    /// <param name="applicableRules">Business rules applicable to this request</param>
    /// <param name="productCosts">Dictionary of product costs (ProductId -> Cost)</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateDiscountRequest(
        DiscountRequest discountRequest,
        Customer customer,
        User salesperson,
        IEnumerable<BusinessRule> applicableRules,
        IDictionary<Guid, Money> productCosts)
    {
        if (discountRequest == null)
            throw new ArgumentNullException(nameof(discountRequest));

        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        if (salesperson == null)
            throw new ArgumentNullException(nameof(salesperson));

        var result = new ValidationResult();

        // Rule 1: Blocked customer → automatic rejection
        var customerValidation = ValidateCustomerStatus(customer);
        result.Merge(customerValidation);

        // If customer is blocked, no need to check other rules
        if (!customerValidation.IsValid)
            return result;

        // Rule 2: Discount cannot exceed limit by user role
        if (applicableRules != null)
        {
            var discountLimitValidation = ValidateDiscountLimits(
                discountRequest.RequestedDiscountPercentage,
                salesperson.Role,
                applicableRules);
            result.Merge(discountLimitValidation);
        }

        // Rule 3: Margin cannot fall below configured minimum
        if (applicableRules != null && productCosts != null)
        {
            var marginValidation = ValidateMinimumMargin(
                discountRequest,
                applicableRules,
                productCosts);
            result.Merge(marginValidation);
        }

        return result;
    }

    /// <summary>
    /// Validates customer status
    /// Business rule: Blocked customers cannot receive new discount requests
    /// </summary>
    /// <param name="customer">Customer to validate</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateCustomerStatus(Customer customer)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer));

        var result = new ValidationResult();

        if (customer.IsBlocked())
        {
            result.AddError($"Customer '{customer.Name}' is blocked and cannot receive discount requests");
            return result;
        }

        if (!customer.CanReceiveDiscountRequests())
        {
            result.AddError($"Customer '{customer.Name}' (status: {customer.Status}) cannot receive discount requests");
            return result;
        }

        return result;
    }

    /// <summary>
    /// Validates discount limits based on business rules
    /// Business rule: Discount cannot exceed limit by user role
    /// </summary>
    /// <param name="requestedDiscountPercentage">Requested discount percentage</param>
    /// <param name="userRole">User role</param>
    /// <param name="applicableRules">Applicable business rules</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateDiscountLimits(
        decimal requestedDiscountPercentage,
        UserRole userRole,
        IEnumerable<BusinessRule> applicableRules)
    {
        var result = new ValidationResult();

        if (applicableRules == null || !applicableRules.Any())
            return result;

        // Get active discount limit rules
        var discountLimitRules = applicableRules
            .Where(r => r.IsActive && r.IsDiscountLimitRule())
            .OrderBy(r => r.Priority)
            .ToList();

        if (!discountLimitRules.Any())
            return result;

        // Find the most restrictive discount limit
        decimal? maxDiscountAllowed = null;
        BusinessRule? violatedRule = null;

        foreach (var rule in discountLimitRules)
        {
            // Check if rule applies to this user role
            if (rule.IsUserRoleScope() && rule.TargetIdentifier != userRole.ToString())
                continue;

            // Parse the discount limit from parameters
            // Expected format: {"maxDiscountPercentage": 20.0}
            var maxDiscount = ParseMaxDiscountFromRule(rule);
            if (maxDiscount.HasValue)
            {
                if (!maxDiscountAllowed.HasValue || maxDiscount.Value < maxDiscountAllowed.Value)
                {
                    maxDiscountAllowed = maxDiscount.Value;
                    violatedRule = rule;
                }
            }
        }

        if (maxDiscountAllowed.HasValue && requestedDiscountPercentage > maxDiscountAllowed.Value)
        {
            result.AddError(
                $"Requested discount of {requestedDiscountPercentage:N2}% exceeds the maximum allowed discount of {maxDiscountAllowed.Value:N2}% " +
                $"for {userRole} role (Rule: {violatedRule?.Name})");
        }

        return result;
    }

    /// <summary>
    /// Validates minimum margin requirements based on business rules
    /// Business rule: Margin cannot fall below configured minimum
    /// </summary>
    /// <param name="discountRequest">Discount request</param>
    /// <param name="applicableRules">Applicable business rules</param>
    /// <param name="productCosts">Dictionary of product costs</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateMinimumMargin(
        DiscountRequest discountRequest,
        IEnumerable<BusinessRule> applicableRules,
        IDictionary<Guid, Money> productCosts)
    {
        var result = new ValidationResult();

        if (applicableRules == null || !applicableRules.Any())
            return result;

        if (productCosts == null || !productCosts.Any())
        {
            result.AddWarning("Product costs not provided - margin validation skipped");
            return result;
        }

        // Get active minimum margin rules
        var minimumMarginRules = applicableRules
            .Where(r => r.IsActive && r.IsMinimumMarginRule())
            .OrderBy(r => r.Priority)
            .ToList();

        if (!minimumMarginRules.Any())
            return result;

        // Validate margin for each item
        foreach (var item in discountRequest.Items)
        {
            if (!productCosts.TryGetValue(item.ProductId, out var productCost))
            {
                result.AddWarning($"Cost not found for product '{item.ProductName}' - margin validation skipped");
                continue;
            }

            var finalPrice = item.UnitFinalPrice;
            var actualMargin = _marginCalculationService.CalculateMarginPercentage(finalPrice, productCost);

            // Find applicable minimum margin rule for this product
            decimal? minimumMarginRequired = null;
            BusinessRule? violatedRule = null;

            foreach (var rule in minimumMarginRules)
            {
                // Check if rule applies to this product
                if (rule.IsProductScope() && rule.TargetEntityId != item.ProductId)
                    continue;

                // Parse minimum margin from parameters
                // Expected format: {"minimumMarginPercentage": 15.0}
                var minMargin = ParseMinimumMarginFromRule(rule);
                if (minMargin.HasValue)
                {
                    if (!minimumMarginRequired.HasValue || minMargin.Value > minimumMarginRequired.Value)
                    {
                        minimumMarginRequired = minMargin.Value;
                        violatedRule = rule;
                    }
                }
            }

            if (minimumMarginRequired.HasValue && actualMargin < minimumMarginRequired.Value)
            {
                result.AddError(
                    $"Product '{item.ProductName}': Margin of {actualMargin:N2}% is below the minimum required margin of {minimumMarginRequired.Value:N2}% " +
                    $"(Rule: {violatedRule?.Name})");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates product availability for discount requests
    /// </summary>
    /// <param name="products">Products in the request</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateProductAvailability(IEnumerable<Product> products)
    {
        var result = new ValidationResult();

        if (products == null || !products.Any())
        {
            result.AddError("No products provided");
            return result;
        }

        foreach (var product in products)
        {
            if (!product.CanBeIncludedInDiscountRequests())
            {
                result.AddError($"Product '{product.Name}' (status: {product.Status}) cannot be included in discount requests");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates if user has permission to create discount requests
    /// </summary>
    /// <param name="user">User to validate</param>
    /// <returns>Validation result</returns>
    public ValidationResult ValidateUserPermissions(User user)
    {
        var result = new ValidationResult();

        if (user == null)
        {
            result.AddError("User not found");
            return result;
        }

        if (!user.IsActive())
        {
            result.AddError($"User '{user.Name}' is not active");
        }

        if (user.IsBlocked())
        {
            result.AddError($"User '{user.Name}' is blocked");
        }

        // Salespeople and managers can create discount requests
        if (!user.IsSalesperson() && !user.IsManager() && !user.IsAdmin())
        {
            result.AddError($"User '{user.Name}' does not have permission to create discount requests");
        }

        return result;
    }

    /// <summary>
    /// Checks if auto-approval is allowed based on business rules
    /// </summary>
    /// <param name="discountRequest">Discount request</param>
    /// <param name="applicableRules">Applicable business rules</param>
    /// <param name="riskScore">Risk score calculated by AI</param>
    /// <returns>True if auto-approval is allowed, false otherwise</returns>
    public bool IsAutoApprovalAllowed(
        DiscountRequest discountRequest,
        IEnumerable<BusinessRule> applicableRules,
        decimal riskScore)
    {
        if (applicableRules == null || !applicableRules.Any())
            return false;

        // Get active auto-approval rules
        var autoApprovalRules = applicableRules
            .Where(r => r.IsActive && r.IsAutoApprovalRule())
            .OrderBy(r => r.Priority)
            .ToList();

        if (!autoApprovalRules.Any())
            return false;

        foreach (var rule in autoApprovalRules)
        {
            var criteria = ParseAutoApprovalCriteria(rule);
            if (criteria == null)
                continue;

            // Check all criteria
            bool meetsAllCriteria = true;

            if (criteria.MaxDiscountPercentage.HasValue &&
                discountRequest.RequestedDiscountPercentage > criteria.MaxDiscountPercentage.Value)
            {
                meetsAllCriteria = false;
            }

            if (criteria.MaxRiskScore.HasValue && riskScore > criteria.MaxRiskScore.Value)
            {
                meetsAllCriteria = false;
            }

            if (meetsAllCriteria)
                return true;
        }

        return false;
    }

    // Helper methods to parse rule parameters
    private decimal? ParseMaxDiscountFromRule(BusinessRule rule)
    {
        try
        {
            // Simple JSON parsing - in production use System.Text.Json or Newtonsoft.Json
            var parameters = rule.Parameters;
            var match = System.Text.RegularExpressions.Regex.Match(parameters, @"""maxDiscountPercentage""\s*:\s*([\d.]+)");
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

    private decimal? ParseMinimumMarginFromRule(BusinessRule rule)
    {
        try
        {
            var parameters = rule.Parameters;
            var match = System.Text.RegularExpressions.Regex.Match(parameters, @"""minimumMarginPercentage""\s*:\s*([\d.]+)");
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

    private AutoApprovalCriteria? ParseAutoApprovalCriteria(BusinessRule rule)
    {
        try
        {
            var parameters = rule.Parameters;
            var criteria = new AutoApprovalCriteria();

            var maxDiscountMatch = System.Text.RegularExpressions.Regex.Match(parameters, @"""maxDiscountPercentage""\s*:\s*([\d.]+)");
            if (maxDiscountMatch.Success && decimal.TryParse(maxDiscountMatch.Groups[1].Value, out var maxDiscount))
            {
                criteria.MaxDiscountPercentage = maxDiscount;
            }

            var maxRiskMatch = System.Text.RegularExpressions.Regex.Match(parameters, @"""maxRiskScore""\s*:\s*([\d.]+)");
            if (maxRiskMatch.Success && decimal.TryParse(maxRiskMatch.Groups[1].Value, out var maxRisk))
            {
                criteria.MaxRiskScore = maxRisk;
            }

            var minMarginMatch = System.Text.RegularExpressions.Regex.Match(parameters, @"""minMarginPercentage""\s*:\s*([\d.]+)");
            if (minMarginMatch.Success && decimal.TryParse(minMarginMatch.Groups[1].Value, out var minMargin))
            {
                criteria.MinMarginPercentage = minMargin;
            }

            return criteria;
        }
        catch
        {
            return null;
        }
    }

    private class AutoApprovalCriteria
    {
        public decimal? MaxDiscountPercentage { get; set; }
        public decimal? MaxRiskScore { get; set; }
        public decimal? MinMarginPercentage { get; set; }
    }
}
