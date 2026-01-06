using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Request to query discount request history with filters
/// </summary>
public class QueryDiscountRequestHistoryRequest
{
    /// <summary>
    /// Company ID (for multi-tenant isolation)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// User ID making the request (for audit and permissions)
    /// </summary>
    public Guid RequestedBy { get; set; }

    /// <summary>
    /// Filter by customer ID (optional)
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Filter by salesperson ID (optional)
    /// </summary>
    public Guid? SalespersonId { get; set; }

    /// <summary>
    /// Filter by status (optional)
    /// </summary>
    public DiscountRequestStatus? Status { get; set; }

    /// <summary>
    /// Filter by approval source: Human, AI, or both (optional)
    /// </summary>
    public ApprovalSource? ApprovalSource { get; set; }

    /// <summary>
    /// Filter by date range - start date (optional)
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by date range - end date (optional)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by minimum discount percentage (optional)
    /// </summary>
    public decimal? MinDiscountPercentage { get; set; }

    /// <summary>
    /// Filter by maximum discount percentage (optional)
    /// </summary>
    public decimal? MaxDiscountPercentage { get; set; }

    /// <summary>
    /// Filter by minimum risk score (optional)
    /// </summary>
    public decimal? MinRiskScore { get; set; }

    /// <summary>
    /// Filter by maximum risk score (optional)
    /// </summary>
    public decimal? MaxRiskScore { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Sort direction: Asc or Desc
    /// </summary>
    public string SortDirection { get; set; } = "Desc";

    /// <summary>
    /// Include statistics in the response
    /// </summary>
    public bool IncludeStatistics { get; set; } = true;
}

/// <summary>
/// Response with discount request history and statistics
/// </summary>
public class QueryDiscountRequestHistoryResponse
{
    /// <summary>
    /// List of discount requests matching the filters
    /// </summary>
    public List<DiscountRequestSummary> Requests { get; set; } = new();

    /// <summary>
    /// Total count of requests matching filters (for pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Statistics for the filtered results
    /// </summary>
    public HistoryStatistics? Statistics { get; set; }
}

/// <summary>
/// Summary of a discount request for history listing
/// </summary>
public class DiscountRequestSummary
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SalespersonName { get; set; } = string.Empty;
    public decimal RequestedDiscountPercentage { get; set; }
    public decimal? EstimatedMarginPercentage { get; set; }
    public decimal? RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DiscountRequestStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DecisionDateTime { get; set; }
    public int? SlaTimeInSeconds { get; set; }
    public string? SlaTimeFormatted { get; set; }
    public bool WasAutoApproved { get; set; }
    public ApprovalSource? ApprovalSource { get; set; }
    public string? ApproverName { get; set; }
    public ApprovalDecision? Decision { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Statistics for the history query results
/// </summary>
public class HistoryStatistics
{
    /// <summary>
    /// Total requests in the filtered set
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Requests approved
    /// </summary>
    public int ApprovedRequests { get; set; }

    /// <summary>
    /// Requests rejected
    /// </summary>
    public int RejectedRequests { get; set; }

    /// <summary>
    /// Requests pending (under analysis)
    /// </summary>
    public int PendingRequests { get; set; }

    /// <summary>
    /// Requests auto-approved by AI
    /// </summary>
    public int AutoApprovedRequests { get; set; }

    /// <summary>
    /// Auto-approval rate (%)
    /// </summary>
    public decimal AutoApprovalRate { get; set; }

    /// <summary>
    /// Average discount percentage
    /// </summary>
    public decimal AverageDiscountPercentage { get; set; }

    /// <summary>
    /// Average estimated margin percentage
    /// </summary>
    public decimal AverageMarginPercentage { get; set; }

    /// <summary>
    /// Average risk score
    /// </summary>
    public decimal AverageRiskScore { get; set; }

    /// <summary>
    /// Average SLA time in seconds
    /// </summary>
    public int AverageSlaTimeInSeconds { get; set; }

    /// <summary>
    /// Average SLA time formatted
    /// </summary>
    public string AverageSlaTimeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Comparison between human and AI approvals
    /// </summary>
    public HumanVsAIComparison HumanVsAI { get; set; } = new();

    /// <summary>
    /// Distribution by risk level
    /// </summary>
    public RiskDistribution RiskDistribution { get; set; } = new();
}

/// <summary>
/// Comparison statistics between human and AI approvals
/// </summary>
public class HumanVsAIComparison
{
    public int HumanApprovals { get; set; }
    public int AIApprovals { get; set; }
    public decimal HumanApprovalRate { get; set; }
    public decimal AIApprovalRate { get; set; }
    public int AverageHumanSlaSeconds { get; set; }
    public int AverageAISlaSeconds { get; set; }
    public string AverageHumanSlaFormatted { get; set; } = string.Empty;
    public string AverageAISlaFormatted { get; set; } = string.Empty;
    public decimal AverageHumanRiskScore { get; set; }
    public decimal AverageAIRiskScore { get; set; }
}

/// <summary>
/// Distribution of requests by risk level
/// </summary>
public class RiskDistribution
{
    public int LowRisk { get; set; }
    public int MediumRisk { get; set; }
    public int HighRisk { get; set; }
    public int VeryHighRisk { get; set; }
    public decimal LowRiskPercentage { get; set; }
    public decimal MediumRiskPercentage { get; set; }
    public decimal HighRiskPercentage { get; set; }
    public decimal VeryHighRiskPercentage { get; set; }
}
