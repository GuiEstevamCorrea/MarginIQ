namespace Domain.Enums;

/// <summary>
/// Represents the source/origin of an approval decision
/// </summary>
public enum ApprovalSource
{
    /// <summary>
    /// Decision made by a human (manager or admin)
    /// </summary>
    Human = 1,
    
    /// <summary>
    /// Decision made automatically by AI
    /// </summary>
    AI = 2
}
