namespace Domain.Enums;

/// <summary>
/// Represents the decision made on a discount request approval
/// </summary>
public enum ApprovalDecision
{
    /// <summary>
    /// Approve the discount request
    /// </summary>
    Approve = 1,
    
    /// <summary>
    /// Reject the discount request
    /// </summary>
    Reject = 2,
    
    /// <summary>
    /// Request adjustment to the discount request
    /// </summary>
    RequestAdjustment = 3
}
