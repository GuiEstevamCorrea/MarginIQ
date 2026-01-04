namespace Domain.Enums;

/// <summary>
/// Represents the role/profile of a user in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Sales representative - can create discount requests
    /// </summary>
    Salesperson = 1,
    
    /// <summary>
    /// Manager - can approve/reject discount requests
    /// </summary>
    Manager = 2,
    
    /// <summary>
    /// Administrator - full system access and configuration
    /// </summary>
    Admin = 3
}
