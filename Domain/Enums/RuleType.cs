namespace Domain.Enums;

/// <summary>
/// Represents the type of business rule
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Minimum margin rule - enforces a minimum profit margin percentage
    /// </summary>
    MinimumMargin = 1,
    
    /// <summary>
    /// Discount limit rule - sets maximum discount percentage allowed
    /// </summary>
    DiscountLimit = 2,
    
    /// <summary>
    /// Auto-approval rule - defines conditions for automatic approval by AI
    /// </summary>
    AutoApproval = 3
}
