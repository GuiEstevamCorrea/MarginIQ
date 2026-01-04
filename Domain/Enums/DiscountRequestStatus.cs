namespace Domain.Enums;

/// <summary>
/// Represents the status of a discount request in the approval workflow
/// </summary>
public enum DiscountRequestStatus
{
    /// <summary>
    /// Under analysis - waiting for approval decision
    /// </summary>
    UnderAnalysis = 1,
    
    /// <summary>
    /// Approved by manager or admin
    /// </summary>
    Approved = 2,
    
    /// <summary>
    /// Rejected by manager or admin
    /// </summary>
    Rejected = 3,
    
    /// <summary>
    /// Adjustment requested - needs modification before resubmission
    /// </summary>
    AdjustmentRequested = 4,
    
    /// <summary>
    /// Auto-approved by AI without human intervention
    /// </summary>
    AutoApprovedByAI = 5
}
