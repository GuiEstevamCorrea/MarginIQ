namespace Domain.Enums;

/// <summary>
/// Represents the origin/source of an audit log entry
/// </summary>
public enum AuditOrigin
{
    /// <summary>
    /// Action performed by a human user
    /// </summary>
    Human = 1,
    
    /// <summary>
    /// Action performed by AI/system automatically
    /// </summary>
    AI = 2,
    
    /// <summary>
    /// Action performed by system/background process
    /// </summary>
    System = 3
}
