using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case: Get AI Decision Audit Report (5.4 – Governança da IA)
/// Provides complete audit trail of AI decisions.
/// Enables governance by showing:
/// - All decisions made by AI vs Human
/// - Auto-approvals and their outcomes
/// - AI performance metrics
/// - Decision patterns over time
/// </summary>
public class GetAIDecisionAuditReportUseCase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IAILearningDataRepository _learningDataRepository;

    public GetAIDecisionAuditReportUseCase(
        IAuditLogRepository auditLogRepository,
        IApprovalRepository approvalRepository,
        IDiscountRequestRepository discountRequestRepository,
        IAILearningDataRepository learningDataRepository)
    {
        _auditLogRepository = auditLogRepository;
        _approvalRepository = approvalRepository;
        _discountRequestRepository = discountRequestRepository;
        _learningDataRepository = learningDataRepository;
    }

    /// <summary>
    /// Generates AI decision audit report for a company
    /// </summary>
    public async Task<AIDecisionAuditReport> ExecuteAsync(
        Guid companyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30); // Default: last 30 days
        var to = toDate ?? DateTime.UtcNow;

        var report = new AIDecisionAuditReport
        {
            CompanyId = companyId,
            FromDate = from,
            ToDate = to,
            GeneratedAt = DateTime.UtcNow
        };

        // Get all approvals in date range
        var allApprovals = (await _approvalRepository.GetByCompanyIdAsync(
            companyId,
            cancellationToken)).ToList();

        var approvalsInRange = allApprovals
            .Where(a => a.DecisionDateTime >= from && a.DecisionDateTime <= to)
            .ToList();

        // Calculate metrics
        var aiApprovals = approvalsInRange
            .Where(a => a.Source == ApprovalSource.AI)
            .ToList();

        var humanApprovals = approvalsInRange
            .Where(a => a.Source == ApprovalSource.Human)
            .ToList();

        report.TotalDecisions = approvalsInRange.Count;
        report.AIDecisions = aiApprovals.Count;
        report.HumanDecisions = humanApprovals.Count;

        // AI decision breakdown
        report.AIAutoApprovals = aiApprovals.Count(a => a.Decision == ApprovalDecision.Approve);
        report.AIRejections = aiApprovals.Count(a => a.Decision == ApprovalDecision.Reject);

        // Human decision breakdown
        report.HumanApprovals = humanApprovals.Count(a => a.Decision == ApprovalDecision.Approve);
        report.HumanRejections = humanApprovals.Count(a => a.Decision == ApprovalDecision.Reject);

        // Calculate percentages
        if (report.TotalDecisions > 0)
        {
            report.AIDecisionPercentage = (decimal)report.AIDecisions / report.TotalDecisions * 100;
            report.AutoApprovalRate = report.AIDecisions > 0
                ? (decimal)report.AIAutoApprovals / report.AIDecisions * 100
                : 0;
        }

        // Get learning data to analyze outcomes
        var learningData = (await _learningDataRepository.GetByDateRangeAsync(
            companyId,
            from,
            to)).ToList();

        // Calculate AI accuracy (decisions that led to successful sales)
        var aiLearningData = learningData
            .Where(d => d.DecisionSource == ApprovalSource.AI)
            .ToList();

        if (aiLearningData.Any(d => d.SaleOutcome.HasValue))
        {
            var aiDecisionsWithOutcome = aiLearningData.Where(d => d.SaleOutcome.HasValue).ToList();
            var successfulAIDecisions = aiDecisionsWithOutcome.Count(d =>
                (d.Decision == ApprovalDecision.Approve && d.SaleOutcome == true) ||
                (d.Decision == ApprovalDecision.Reject && d.SaleOutcome == false));

            report.AIAccuracyRate = (decimal)successfulAIDecisions / aiDecisionsWithOutcome.Count * 100;
        }

        // Get recent AI decisions for timeline
        report.RecentDecisions = aiApprovals
            .OrderByDescending(a => a.DecisionDateTime)
            .Take(20)
            .Select(a => new AIDecisionSummary
            {
                DecisionId = a.Id,
                DiscountRequestId = a.DiscountRequestId,
                Decision = a.Decision.ToString(),
                CreatedAt = a.DecisionDateTime,
                ProcessingTimeSec = a.SlaTimeInSeconds
            })
            .ToList();

        // Get audit logs for governance changes
        var governanceAudits = (await _auditLogRepository.GetByEntityNameAsync(
            companyId,
            "AIGovernanceSettings",
            cancellationToken)).ToList();

        report.GovernanceChanges = governanceAudits
            .Where(a => a.DateTime >= from && a.DateTime <= to)
            .OrderByDescending(a => a.DateTime)
            .Take(10)
            .Select(a => new GovernanceChangeSummary
            {
                ChangeId = a.Id,
                Action = a.Action.ToString(),
                ChangedBy = a.UserId,
                ChangedAt = a.DateTime,
                Details = a.Payload ?? string.Empty
            })
            .ToList();

        // Performance metrics
        report.AverageProcessingTimeSec = aiApprovals.Any()
            ? (decimal)aiApprovals.Average(a => a.SlaTimeInSeconds)
            : 0;

        report.FastestDecisionSec = aiApprovals.Any()
            ? aiApprovals.Min(a => a.SlaTimeInSeconds)
            : 0;

        report.SlowestDecisionSec = aiApprovals.Any()
            ? aiApprovals.Max(a => a.SlaTimeInSeconds)
            : 0;

        return report;
    }
}

/// <summary>
/// AI decision audit report
/// </summary>
public class AIDecisionAuditReport
{
    public Guid CompanyId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }

    // Overall metrics
    public int TotalDecisions { get; set; }
    public int AIDecisions { get; set; }
    public int HumanDecisions { get; set; }
    public decimal AIDecisionPercentage { get; set; }

    // AI decision breakdown
    public int AIAutoApprovals { get; set; }
    public int AIRejections { get; set; }
    public decimal AutoApprovalRate { get; set; }

    // Human decision breakdown
    public int HumanApprovals { get; set; }
    public int HumanRejections { get; set; }

    // AI performance
    public decimal AIAccuracyRate { get; set; }
    public decimal AverageProcessingTimeSec { get; set; }
    public decimal FastestDecisionSec { get; set; }
    public decimal SlowestDecisionSec { get; set; }

    // Recent decisions
    public List<AIDecisionSummary> RecentDecisions { get; set; } = new();

    // Governance changes
    public List<GovernanceChangeSummary> GovernanceChanges { get; set; } = new();

    // Summary text
    public string Summary => BuildSummary();

    private string BuildSummary()
    {
        if (TotalDecisions == 0)
            return "No decisions recorded in this period";

        var parts = new List<string>
        {
            $"{TotalDecisions} total decisions",
            $"{AIDecisionPercentage:F1}% by AI",
            $"{AutoApprovalRate:F1}% auto-approval rate"
        };

        if (AIAccuracyRate > 0)
        {
            parts.Add($"{AIAccuracyRate:F1}% AI accuracy");
        }

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Summary of an AI decision
/// </summary>
public class AIDecisionSummary
{
    public Guid DecisionId { get; set; }
    public Guid DiscountRequestId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal ProcessingTimeSec { get; set; }
}

/// <summary>
/// Summary of a governance configuration change
/// </summary>
public class GovernanceChangeSummary
{
    public Guid ChangeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Details { get; set; } = string.Empty;
}
