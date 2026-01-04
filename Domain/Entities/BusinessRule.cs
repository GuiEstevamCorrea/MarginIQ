using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a Business Rule that governs discount approval policies.
/// Rules define constraints like minimum margin, discount limits, and auto-approval criteria.
/// Business rules: 
/// - Discount cannot exceed limit by role
/// - Margin cannot fall below configured minimum
/// - Blocked customer → automatic rejection
/// </summary>
public class BusinessRule
{
    /// <summary>
    /// Unique identifier for the business rule
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Name/description of the rule
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Type of rule (MinimumMargin, DiscountLimit, AutoApproval)
    /// </summary>
    public RuleType Type { get; private set; }

    /// <summary>
    /// Scope of the rule (Product, Category, Customer, UserRole, Global)
    /// </summary>
    public RuleScope Scope { get; private set; }

    /// <summary>
    /// Target entity ID for the scope (ProductId, CustomerId, etc.)
    /// Null for Global scope or UserRole scope
    /// </summary>
    public Guid? TargetEntityId { get; private set; }

    /// <summary>
    /// Additional target identifier (e.g., Category name, User role)
    /// Used when TargetEntityId is not applicable
    /// </summary>
    public string? TargetIdentifier { get; private set; }

    /// <summary>
    /// Rule parameters stored as JSON
    /// Examples:
    /// - MinimumMargin: {"minimumMarginPercentage": 15.0}
    /// - DiscountLimit: {"maxDiscountPercentage": 20.0}
    /// - AutoApproval: {"maxDiscountPercentage": 10.0, "minMarginPercentage": 20.0, "maxRiskScore": 30.0}
    /// </summary>
    public string Parameters { get; private set; }

    /// <summary>
    /// The company (tenant) this business rule belongs to
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Whether the rule is currently active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Priority/order of rule execution (lower number = higher priority)
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Date and time when the rule was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the rule was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// User who created the rule
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    // Navigation property
    public Company? Company { get; private set; }
    public User? CreatedByUser { get; private set; }

    // Private constructor for EF Core
    private BusinessRule() 
    {
        Name = string.Empty;
        Parameters = string.Empty;
    }

    /// <summary>
    /// Creates a new BusinessRule instance
    /// </summary>
    /// <param name="name">Rule name/description</param>
    /// <param name="type">Rule type</param>
    /// <param name="scope">Rule scope</param>
    /// <param name="parameters">Rule parameters as JSON</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="priority">Priority (default 100)</param>
    /// <param name="targetEntityId">Optional target entity ID</param>
    /// <param name="targetIdentifier">Optional target identifier</param>
    /// <param name="createdByUserId">Optional user who created the rule</param>
    public BusinessRule(
        string name,
        RuleType type,
        RuleScope scope,
        string parameters,
        Guid companyId,
        int priority = 100,
        Guid? targetEntityId = null,
        string? targetIdentifier = null,
        Guid? createdByUserId = null)
    {
        ValidateName(name);
        ValidateParameters(parameters);
        ValidatePriority(priority);
        ValidateScope(scope, targetEntityId, targetIdentifier);

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        Scope = scope;
        TargetEntityId = targetEntityId;
        TargetIdentifier = targetIdentifier;
        Parameters = parameters;
        CompanyId = companyId;
        Priority = priority;
        IsActive = true; // New rules start active
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
    }

    /// <summary>
    /// Activates the business rule
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            throw new InvalidOperationException("Business rule is already active");

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the business rule
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Business rule is already inactive");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule name
    /// </summary>
    /// <param name="name">New name</param>
    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule parameters
    /// </summary>
    /// <param name="parameters">New parameters JSON</param>
    public void UpdateParameters(string parameters)
    {
        ValidateParameters(parameters);
        Parameters = parameters;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the rule priority
    /// </summary>
    /// <param name="priority">New priority</param>
    public void UpdatePriority(int priority)
    {
        ValidatePriority(priority);
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the target entity ID
    /// </summary>
    /// <param name="targetEntityId">New target entity ID</param>
    public void UpdateTargetEntityId(Guid? targetEntityId)
    {
        TargetEntityId = targetEntityId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the target identifier
    /// </summary>
    /// <param name="targetIdentifier">New target identifier</param>
    public void UpdateTargetIdentifier(string? targetIdentifier)
    {
        TargetIdentifier = targetIdentifier;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the rule applies to a specific target
    /// </summary>
    /// <param name="entityId">Entity ID to check</param>
    /// <param name="identifier">Identifier to check (category, role, etc.)</param>
    /// <returns>True if the rule applies to the target</returns>
    public bool AppliesTo(Guid? entityId, string? identifier = null)
    {
        if (Scope == RuleScope.Global)
            return true;

        if (TargetEntityId.HasValue && entityId.HasValue && TargetEntityId.Value == entityId.Value)
            return true;

        if (!string.IsNullOrWhiteSpace(TargetIdentifier) && 
            !string.IsNullOrWhiteSpace(identifier) && 
            TargetIdentifier.Equals(identifier, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if this is a minimum margin rule
    /// </summary>
    public bool IsMinimumMarginRule() => Type == RuleType.MinimumMargin;

    /// <summary>
    /// Checks if this is a discount limit rule
    /// </summary>
    public bool IsDiscountLimitRule() => Type == RuleType.DiscountLimit;

    /// <summary>
    /// Checks if this is an auto-approval rule
    /// </summary>
    public bool IsAutoApprovalRule() => Type == RuleType.AutoApproval;

    /// <summary>
    /// Checks if the rule has global scope
    /// </summary>
    public bool IsGlobalScope() => Scope == RuleScope.Global;

    /// <summary>
    /// Checks if the rule applies to products
    /// </summary>
    public bool IsProductScope() => Scope == RuleScope.Product;

    /// <summary>
    /// Checks if the rule applies to categories
    /// </summary>
    public bool IsCategoryScope() => Scope == RuleScope.Category;

    /// <summary>
    /// Checks if the rule applies to customers
    /// </summary>
    public bool IsCustomerScope() => Scope == RuleScope.Customer;

    /// <summary>
    /// Checks if the rule applies to user roles
    /// </summary>
    public bool IsUserRoleScope() => Scope == RuleScope.UserRole;

    /// <summary>
    /// Gets a description of the rule's scope and target
    /// </summary>
    public string GetScopeDescription()
    {
        return Scope switch
        {
            RuleScope.Global => "Global (All)",
            RuleScope.Product => $"Product: {TargetEntityId}",
            RuleScope.Category => $"Category: {TargetIdentifier ?? "N/A"}",
            RuleScope.Customer => $"Customer: {TargetEntityId}",
            RuleScope.UserRole => $"User Role: {TargetIdentifier ?? "N/A"}",
            _ => "Unknown"
        };
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Business rule name cannot be empty", nameof(name));

        if (name.Length < 3)
            throw new ArgumentException("Business rule name must have at least 3 characters", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Business rule name cannot exceed 200 characters", nameof(name));
    }

    private static void ValidateParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
            throw new ArgumentException("Business rule parameters cannot be empty", nameof(parameters));

        // Basic JSON validation - should start with { and end with }
        var trimmed = parameters.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            throw new ArgumentException("Business rule parameters must be valid JSON", nameof(parameters));
    }

    private static void ValidatePriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentException("Priority cannot be negative", nameof(priority));
    }

    private static void ValidateScope(RuleScope scope, Guid? targetEntityId, string? targetIdentifier)
    {
        // For Product, Customer scope, TargetEntityId should be provided
        if ((scope == RuleScope.Product || scope == RuleScope.Customer) && !targetEntityId.HasValue)
            throw new ArgumentException($"TargetEntityId is required for {scope} scope", nameof(targetEntityId));

        // For Category, UserRole scope, TargetIdentifier should be provided
        if ((scope == RuleScope.Category || scope == RuleScope.UserRole) && string.IsNullOrWhiteSpace(targetIdentifier))
            throw new ArgumentException($"TargetIdentifier is required for {scope} scope", nameof(targetIdentifier));
    }
}
