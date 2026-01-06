namespace Application.DTOs;

/// <summary>
/// Request DTO for updating AI governance settings (5.4)
/// </summary>
public class UpdateAIGovernanceRequest
{
    /// <summary>
    /// Company ID (tenant)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Enable/disable AI for this company
    /// </summary>
    public bool AIEnabled { get; set; } = true;

    /// <summary>
    /// Autonomy level (0-100)
    /// 0 = AI only recommends, never auto-approves
    /// 50 = Moderate autonomy (default)
    /// 100 = Full autonomy within guardrails
    /// </summary>
    public int AutonomyLevel { get; set; } = 50;

    /// <summary>
    /// Maximum risk score threshold for auto-approval (0-100)
    /// If risk score exceeds this, requires human approval
    /// </summary>
    public decimal MaxRiskScoreForAutoApproval { get; set; } = 60m;

    /// <summary>
    /// Minimum AI confidence required for auto-approval (0-1)
    /// If AI confidence is below this, requires human approval
    /// </summary>
    public decimal MinConfidenceForAutoApproval { get; set; } = 0.75m;

    /// <summary>
    /// Require human review for all AI decisions (overrides auto-approval)
    /// </summary>
    public bool RequireHumanReview { get; set; } = false;

    /// <summary>
    /// Enable AI decision auditing
    /// When enabled, all AI decisions are logged to audit trail
    /// </summary>
    public bool EnableAudit { get; set; } = true;

    /// <summary>
    /// Enable explainability for all AI decisions
    /// When enabled, AI must provide explanation for recommendations
    /// </summary>
    public bool EnableExplainability { get; set; } = true;

    /// <summary>
    /// Maximum discount percentage AI can auto-approve
    /// Discounts exceeding this always require human approval
    /// </summary>
    public decimal MaxAutoApprovalDiscount { get; set; } = 15m;

    /// <summary>
    /// Enable incremental learning
    /// When enabled, AI continuously learns from decisions and outcomes
    /// </summary>
    public bool EnableIncrementalLearning { get; set; } = true;

    /// <summary>
    /// Frequency of model retraining (in days)
    /// Determines how often the AI model is retrained with new data
    /// </summary>
    public int RetrainingFrequencyDays { get; set; } = 30;
}

/// <summary>
/// Response DTO for AI governance settings (5.4)
/// </summary>
public class AIGovernanceResponse
{
    /// <summary>
    /// Company ID
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// AI enabled status
    /// </summary>
    public bool AIEnabled { get; set; }

    /// <summary>
    /// Autonomy level (0-100)
    /// </summary>
    public int AutonomyLevel { get; set; }

    /// <summary>
    /// Autonomy level description
    /// </summary>
    public string AutonomyDescription => AutonomyLevel switch
    {
        < 25 => "Conservative: AI recommends only, requires human approval for all decisions",
        < 50 => "Low: AI can auto-approve low-risk decisions within strict limits",
        < 75 => "Moderate: AI can auto-approve medium-risk decisions within guardrails",
        < 90 => "High: AI has broad autonomy, human approval only for high-risk cases",
        _ => "Full: AI has maximum autonomy within configured guardrails"
    };

    /// <summary>
    /// Maximum risk score for auto-approval
    /// </summary>
    public decimal MaxRiskScoreForAutoApproval { get; set; }

    /// <summary>
    /// Minimum confidence for auto-approval
    /// </summary>
    public decimal MinConfidenceForAutoApproval { get; set; }

    /// <summary>
    /// Require human review flag
    /// </summary>
    public bool RequireHumanReview { get; set; }

    /// <summary>
    /// Audit enabled flag
    /// </summary>
    public bool EnableAudit { get; set; }

    /// <summary>
    /// Explainability enabled flag
    /// </summary>
    public bool EnableExplainability { get; set; }

    /// <summary>
    /// Maximum auto-approval discount percentage
    /// </summary>
    public decimal MaxAutoApprovalDiscount { get; set; }

    /// <summary>
    /// Incremental learning enabled flag
    /// </summary>
    public bool EnableIncrementalLearning { get; set; }

    /// <summary>
    /// Retraining frequency in days
    /// </summary>
    public int RetrainingFrequencyDays { get; set; }

    /// <summary>
    /// Next scheduled retraining date
    /// </summary>
    public DateTime? NextRetrainingDate { get; set; }

    /// <summary>
    /// Last time settings were updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// AI operational status
    /// </summary>
    public AIOperationalStatus Status { get; set; } = new();

    /// <summary>
    /// Governance summary for display
    /// </summary>
    public string Summary => BuildSummary();

    private string BuildSummary()
    {
        if (!AIEnabled)
            return "AI is disabled for this company";

        var parts = new List<string>();

        if (RequireHumanReview)
        {
            parts.Add("All decisions require human review");
        }
        else
        {
            parts.Add($"Auto-approval enabled for risk ≤{MaxRiskScoreForAutoApproval:F0}");
            parts.Add($"discounts ≤{MaxAutoApprovalDiscount:F0}%");
            parts.Add($"confidence ≥{MinConfidenceForAutoApproval:P0}");
        }

        return string.Join(", ", parts);
    }
}

/// <summary>
/// AI operational status information
/// </summary>
public class AIOperationalStatus
{
    /// <summary>
    /// AI service is available and healthy
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Last time AI was checked/used
    /// </summary>
    public DateTime? LastChecked { get; set; }

    /// <summary>
    /// Current model version
    /// </summary>
    public string? ModelVersion { get; set; }

    /// <summary>
    /// Last training date
    /// </summary>
    public DateTime? LastTrainingDate { get; set; }

    /// <summary>
    /// Number of decisions made by AI (lifetime)
    /// </summary>
    public int TotalAIDecisions { get; set; }

    /// <summary>
    /// Number of auto-approvals by AI (lifetime)
    /// </summary>
    public int TotalAutoApprovals { get; set; }

    /// <summary>
    /// AI accuracy rate (if available)
    /// </summary>
    public decimal? AccuracyRate { get; set; }
}

/// <summary>
/// Preset governance configurations for common scenarios
/// </summary>
public static class AIGovernancePresets
{
    /// <summary>
    /// Conservative preset: AI recommends only, all decisions require human approval
    /// </summary>
    public static UpdateAIGovernanceRequest Conservative(Guid companyId) => new()
    {
        CompanyId = companyId,
        AIEnabled = true,
        AutonomyLevel = 10,
        MaxRiskScoreForAutoApproval = 0, // Never auto-approve
        MinConfidenceForAutoApproval = 1.0m, // Impossible to reach
        RequireHumanReview = true,
        EnableAudit = true,
        EnableExplainability = true,
        MaxAutoApprovalDiscount = 0,
        EnableIncrementalLearning = true,
        RetrainingFrequencyDays = 30
    };

    /// <summary>
    /// Balanced preset: Moderate autonomy with reasonable guardrails (recommended)
    /// </summary>
    public static UpdateAIGovernanceRequest Balanced(Guid companyId) => new()
    {
        CompanyId = companyId,
        AIEnabled = true,
        AutonomyLevel = 50,
        MaxRiskScoreForAutoApproval = 60,
        MinConfidenceForAutoApproval = 0.75m,
        RequireHumanReview = false,
        EnableAudit = true,
        EnableExplainability = true,
        MaxAutoApprovalDiscount = 15,
        EnableIncrementalLearning = true,
        RetrainingFrequencyDays = 30
    };

    /// <summary>
    /// Aggressive preset: High autonomy, AI has broad decision-making power
    /// </summary>
    public static UpdateAIGovernanceRequest Aggressive(Guid companyId) => new()
    {
        CompanyId = companyId,
        AIEnabled = true,
        AutonomyLevel = 85,
        MaxRiskScoreForAutoApproval = 80,
        MinConfidenceForAutoApproval = 0.60m,
        RequireHumanReview = false,
        EnableAudit = true,
        EnableExplainability = true,
        MaxAutoApprovalDiscount = 30,
        EnableIncrementalLearning = true,
        RetrainingFrequencyDays = 15
    };

    /// <summary>
    /// Disabled preset: AI completely disabled, manual approval only
    /// </summary>
    public static UpdateAIGovernanceRequest Disabled(Guid companyId) => new()
    {
        CompanyId = companyId,
        AIEnabled = false,
        AutonomyLevel = 0,
        MaxRiskScoreForAutoApproval = 0,
        MinConfidenceForAutoApproval = 1.0m,
        RequireHumanReview = true,
        EnableAudit = true,
        EnableExplainability = false,
        MaxAutoApprovalDiscount = 0,
        EnableIncrementalLearning = false,
        RetrainingFrequencyDays = 0
    };
}
