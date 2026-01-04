using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents an Approval decision for a discount request.
/// Tracks who approved/rejected, when, why, and measures SLA compliance.
/// Business rule: Justification is mandatory when rejecting a request.
/// </summary>
public class Approval
{
    /// <summary>
    /// Unique identifier for the approval
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Discount request this approval is for
    /// </summary>
    public Guid DiscountRequestId { get; private set; }

    /// <summary>
    /// User ID who made the approval decision (null if AI)
    /// </summary>
    public Guid? ApproverId { get; private set; }

    /// <summary>
    /// Decision made (Approve, Reject, RequestAdjustment)
    /// </summary>
    public ApprovalDecision Decision { get; private set; }

    /// <summary>
    /// Source of the approval decision (Human or AI)
    /// </summary>
    public ApprovalSource Source { get; private set; }

    /// <summary>
    /// Justification or comments for the decision
    /// Mandatory when rejecting
    /// </summary>
    public string? Justification { get; private set; }

    /// <summary>
    /// SLA time in seconds - time taken to make the decision
    /// Calculated from request creation to approval decision
    /// </summary>
    public int SlaTimeInSeconds { get; private set; }

    /// <summary>
    /// Date and time when the approval decision was made
    /// </summary>
    public DateTime DecisionDateTime { get; private set; }

    /// <summary>
    /// Additional metadata stored as JSON (e.g., AI confidence score, rules applied)
    /// </summary>
    public string? Metadata { get; private set; }

    // Navigation properties
    public DiscountRequest? DiscountRequest { get; private set; }
    public User? Approver { get; private set; }

    // Private constructor for EF Core
    private Approval() { }

    /// <summary>
    /// Creates a new Approval instance for a human decision
    /// </summary>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="approverId">Approver (User) ID</param>
    /// <param name="decision">Approval decision</param>
    /// <param name="slaTimeInSeconds">SLA time in seconds</param>
    /// <param name="justification">Optional justification (mandatory for rejection)</param>
    /// <param name="metadata">Optional metadata JSON</param>
    public Approval(
        Guid discountRequestId,
        Guid approverId,
        ApprovalDecision decision,
        int slaTimeInSeconds,
        string? justification = null,
        string? metadata = null)
    {
        ValidateSlaTime(slaTimeInSeconds);
        ValidateJustification(decision, justification);

        Id = Guid.NewGuid();
        DiscountRequestId = discountRequestId;
        ApproverId = approverId;
        Decision = decision;
        Source = ApprovalSource.Human;
        Justification = justification;
        SlaTimeInSeconds = slaTimeInSeconds;
        DecisionDateTime = DateTime.UtcNow;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a new Approval instance for an AI decision
    /// </summary>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="decision">Approval decision</param>
    /// <param name="slaTimeInSeconds">SLA time in seconds</param>
    /// <param name="justification">Optional AI explanation</param>
    /// <param name="metadata">Optional metadata JSON (e.g., confidence score, model version)</param>
    /// <returns>Approval instance created by AI</returns>
    public static Approval CreateByAI(
        Guid discountRequestId,
        ApprovalDecision decision,
        int slaTimeInSeconds,
        string? justification = null,
        string? metadata = null)
    {
        ValidateSlaTime(slaTimeInSeconds);

        return new Approval
        {
            Id = Guid.NewGuid(),
            DiscountRequestId = discountRequestId,
            ApproverId = null, // AI has no user ID
            Decision = decision,
            Source = ApprovalSource.AI,
            Justification = justification,
            SlaTimeInSeconds = slaTimeInSeconds,
            DecisionDateTime = DateTime.UtcNow,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Updates the justification
    /// </summary>
    /// <param name="justification">New justification</param>
    public void UpdateJustification(string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Justification cannot be empty", nameof(justification));

        Justification = justification;
    }

    /// <summary>
    /// Updates the metadata
    /// </summary>
    /// <param name="metadata">New metadata JSON</param>
    public void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Checks if this approval was made by a human
    /// </summary>
    /// <returns>True if approved by human, false otherwise</returns>
    public bool IsHumanApproval() => Source == ApprovalSource.Human;

    /// <summary>
    /// Checks if this approval was made by AI
    /// </summary>
    /// <returns>True if approved by AI, false otherwise</returns>
    public bool IsAIApproval() => Source == ApprovalSource.AI;

    /// <summary>
    /// Checks if the decision was to approve
    /// </summary>
    /// <returns>True if approved, false otherwise</returns>
    public bool IsApproved() => Decision == ApprovalDecision.Approve;

    /// <summary>
    /// Checks if the decision was to reject
    /// </summary>
    /// <returns>True if rejected, false otherwise</returns>
    public bool IsRejected() => Decision == ApprovalDecision.Reject;

    /// <summary>
    /// Checks if the decision was to request adjustment
    /// </summary>
    /// <returns>True if adjustment requested, false otherwise</returns>
    public bool IsAdjustmentRequested() => Decision == ApprovalDecision.RequestAdjustment;

    /// <summary>
    /// Gets the SLA time formatted as TimeSpan
    /// </summary>
    /// <returns>TimeSpan representing the SLA time</returns>
    public TimeSpan GetSlaTimeSpan() => TimeSpan.FromSeconds(SlaTimeInSeconds);

    /// <summary>
    /// Gets the SLA time formatted as a readable string (e.g., "2h 30m")
    /// </summary>
    /// <returns>Formatted SLA time string</returns>
    public string GetFormattedSlaTime()
    {
        var timeSpan = GetSlaTimeSpan();
        
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
        
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
        
        return $"{timeSpan.Seconds}s";
    }

    /// <summary>
    /// Checks if the SLA was met (below threshold)
    /// </summary>
    /// <param name="slaThresholdInSeconds">SLA threshold in seconds</param>
    /// <returns>True if SLA was met, false otherwise</returns>
    public bool MeetsSla(int slaThresholdInSeconds)
    {
        return SlaTimeInSeconds <= slaThresholdInSeconds;
    }

    /// <summary>
    /// Gets the approver identifier (User ID or "AI")
    /// </summary>
    /// <returns>User ID as string or "AI"</returns>
    public string GetApproverIdentifier()
    {
        return ApproverId.HasValue ? ApproverId.Value.ToString() : "AI";
    }

    private static void ValidateSlaTime(int slaTimeInSeconds)
    {
        if (slaTimeInSeconds < 0)
            throw new ArgumentException("SLA time cannot be negative", nameof(slaTimeInSeconds));
    }

    private static void ValidateJustification(ApprovalDecision decision, string? justification)
    {
        // Justification is mandatory when rejecting
        if (decision == ApprovalDecision.Reject && string.IsNullOrWhiteSpace(justification))
            throw new ArgumentException("Justification is mandatory when rejecting a discount request", nameof(justification));
    }
}
