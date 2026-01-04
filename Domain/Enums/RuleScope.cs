namespace Domain.Enums;

/// <summary>
/// Represents the scope/target of a business rule
/// </summary>
public enum RuleScope
{
    /// <summary>
    /// Rule applies to a specific product
    /// </summary>
    Product = 1,
    
    /// <summary>
    /// Rule applies to a product category
    /// </summary>
    Category = 2,
    
    /// <summary>
    /// Rule applies to a specific customer
    /// </summary>
    Customer = 3,
    
    /// <summary>
    /// Rule applies based on user role (Salesperson, Manager, etc.)
    /// </summary>
    UserRole = 4,
    
    /// <summary>
    /// Rule applies globally to the entire company
    /// </summary>
    Global = 5
}
