using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use Case UC-04: Query Discount Request History
/// 
/// Allows querying discount request history with advanced filters and statistics.
/// Provides insights into:
/// - All decisions made (human vs AI)
/// - Performance metrics (SLA, approval rates)
/// - Risk distribution
/// - Margin analysis
/// 
/// Business rules:
/// - Multi-tenant isolation (only company's data)
/// - Salespersons can only see their own requests
/// - Managers and Admins can see all requests in the company
/// - Comprehensive filtering and sorting
/// - Pagination for large datasets
/// </summary>
public class QueryDiscountRequestHistoryUseCase
{
    private readonly IDiscountRequestRepository _discountRequestRepository;
    private readonly IApprovalRepository _approvalRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;

    public QueryDiscountRequestHistoryUseCase(
        IDiscountRequestRepository discountRequestRepository,
        IApprovalRepository approvalRepository,
        IUserRepository userRepository,
        ICustomerRepository customerRepository)
    {
        _discountRequestRepository = discountRequestRepository ?? throw new ArgumentNullException(nameof(discountRequestRepository));
        _approvalRepository = approvalRepository ?? throw new ArgumentNullException(nameof(approvalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
    }

    /// <summary>
    /// Executes the query discount request history use case
    /// </summary>
    public async Task<QueryDiscountRequestHistoryResponse> ExecuteAsync(
        QueryDiscountRequestHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Step 1: Validate user and determine permissions
        var requestingUser = await ValidateUserAsync(request.CompanyId, request.RequestedBy, cancellationToken);

        // Step 2: Apply role-based filtering
        var effectiveRequest = ApplyRoleBasedFiltering(request, requestingUser);

        // Step 3: Query discount requests with filters
        var allRequests = await _discountRequestRepository.GetByCompanyIdAsync(
            effectiveRequest.CompanyId,
            cancellationToken);

        var filteredRequests = ApplyFilters(allRequests, effectiveRequest);

        // Step 4: Apply sorting
        var sortedRequests = ApplySorting(filteredRequests, effectiveRequest.SortBy, effectiveRequest.SortDirection);

        // Step 5: Calculate total and apply pagination
        var totalCount = sortedRequests.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / effectiveRequest.PageSize);

        var pagedRequests = sortedRequests
            .Skip((effectiveRequest.PageNumber - 1) * effectiveRequest.PageSize)
            .Take(effectiveRequest.PageSize)
            .ToList();

        // Step 6: Load related data and build summaries
        var summaries = await BuildRequestSummariesAsync(pagedRequests, cancellationToken);

        // Step 7: Calculate statistics if requested
        HistoryStatistics? statistics = null;
        if (effectiveRequest.IncludeStatistics)
        {
            statistics = await CalculateStatisticsAsync(filteredRequests.ToList(), cancellationToken);
        }

        // Step 8: Build response
        return new QueryDiscountRequestHistoryResponse
        {
            Requests = summaries,
            TotalCount = totalCount,
            PageNumber = effectiveRequest.PageNumber,
            PageSize = effectiveRequest.PageSize,
            TotalPages = totalPages,
            Statistics = statistics
        };
    }

    /// <summary>
    /// Validates user and checks permissions
    /// </summary>
    private async Task<User> ValidateUserAsync(
        Guid companyId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User with ID {userId} not found");

        if (user.CompanyId != companyId)
            throw new InvalidOperationException("User does not belong to the specified company");

        if (user.Status != UserStatus.Active)
            throw new InvalidOperationException($"User is not active (status: {user.Status})");

        return user;
    }

    /// <summary>
    /// Applies role-based filtering:
    /// - Salesperson: can only see their own requests
    /// - Manager/Admin: can see all company requests
    /// </summary>
    private QueryDiscountRequestHistoryRequest ApplyRoleBasedFiltering(
        QueryDiscountRequestHistoryRequest request,
        User user)
    {
        // If user is a salesperson, force filter to only their requests
        if (user.Role == UserRole.Salesperson)
        {
            request.SalespersonId = user.Id;
        }

        return request;
    }

    /// <summary>
    /// Applies all filters to the request collection
    /// </summary>
    private IEnumerable<DiscountRequest> ApplyFilters(
        IEnumerable<DiscountRequest> requests,
        QueryDiscountRequestHistoryRequest filters)
    {
        var query = requests.AsEnumerable();

        // Filter by customer
        if (filters.CustomerId.HasValue)
        {
            query = query.Where(r => r.CustomerId == filters.CustomerId.Value);
        }

        // Filter by salesperson
        if (filters.SalespersonId.HasValue)
        {
            query = query.Where(r => r.SalespersonId == filters.SalespersonId.Value);
        }

        // Filter by status
        if (filters.Status.HasValue)
        {
            query = query.Where(r => r.Status == filters.Status.Value);
        }

        // Filter by date range
        if (filters.StartDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= filters.StartDate.Value);
        }

        if (filters.EndDate.HasValue)
        {
            // Include full end date (until 23:59:59)
            var endDate = filters.EndDate.Value.Date.AddDays(1).AddSeconds(-1);
            query = query.Where(r => r.CreatedAt <= endDate);
        }

        // Filter by discount percentage range
        if (filters.MinDiscountPercentage.HasValue)
        {
            query = query.Where(r => r.RequestedDiscountPercentage >= filters.MinDiscountPercentage.Value);
        }

        if (filters.MaxDiscountPercentage.HasValue)
        {
            query = query.Where(r => r.RequestedDiscountPercentage <= filters.MaxDiscountPercentage.Value);
        }

        // Filter by risk score range
        if (filters.MinRiskScore.HasValue)
        {
            query = query.Where(r => r.RiskScore.HasValue && r.RiskScore.Value >= filters.MinRiskScore.Value);
        }

        if (filters.MaxRiskScore.HasValue)
        {
            query = query.Where(r => r.RiskScore.HasValue && r.RiskScore.Value <= filters.MaxRiskScore.Value);
        }

        // Filter by approval source (requires loading approvals - will be handled separately if needed)
        // For now, we can filter by AutoApprovedByAI status as a proxy for AI approvals
        if (filters.ApprovalSource.HasValue)
        {
            if (filters.ApprovalSource.Value == Domain.Enums.ApprovalSource.AI)
            {
                query = query.Where(r => r.Status == DiscountRequestStatus.AutoApprovedByAI);
            }
            else if (filters.ApprovalSource.Value == Domain.Enums.ApprovalSource.Human)
            {
                query = query.Where(r => r.Status == DiscountRequestStatus.Approved || 
                                        r.Status == DiscountRequestStatus.Rejected ||
                                        r.Status == DiscountRequestStatus.AdjustmentRequested);
            }
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the request collection
    /// </summary>
    private IEnumerable<DiscountRequest> ApplySorting(
        IEnumerable<DiscountRequest> requests,
        string sortBy,
        string sortDirection)
    {
        var isAscending = sortDirection.Equals("Asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLower() switch
        {
            "createdat" => isAscending 
                ? requests.OrderBy(r => r.CreatedAt)
                : requests.OrderByDescending(r => r.CreatedAt),
            
            "discount" or "discountpercentage" => isAscending
                ? requests.OrderBy(r => r.RequestedDiscountPercentage)
                : requests.OrderByDescending(r => r.RequestedDiscountPercentage),
            
            "margin" or "marginpercentage" => isAscending
                ? requests.OrderBy(r => r.EstimatedMarginPercentage ?? 0)
                : requests.OrderByDescending(r => r.EstimatedMarginPercentage ?? 0),
            
            "riskscore" or "risk" => isAscending
                ? requests.OrderBy(r => r.RiskScore ?? 0)
                : requests.OrderByDescending(r => r.RiskScore ?? 0),
            
            "status" => isAscending
                ? requests.OrderBy(r => r.Status)
                : requests.OrderByDescending(r => r.Status),
            
            _ => requests.OrderByDescending(r => r.CreatedAt) // Default: newest first
        };
    }

    /// <summary>
    /// Builds request summaries with related data
    /// </summary>
    private async Task<List<DiscountRequestSummary>> BuildRequestSummariesAsync(
        List<DiscountRequest> requests,
        CancellationToken cancellationToken)
    {
        var summaries = new List<DiscountRequestSummary>();

        // Load all related customers and users
        var customerIds = requests.Select(r => r.CustomerId).Distinct().ToList();
        var salespersonIds = requests.Select(r => r.SalespersonId).Distinct().ToList();
        
        var customers = new Dictionary<Guid, Customer>();
        foreach (var customerId in customerIds)
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer != null)
            {
                customers[customerId] = customer;
            }
        }

        var salespeople = new Dictionary<Guid, User>();
        foreach (var salespersonId in salespersonIds)
        {
            var salesperson = await _userRepository.GetByIdAsync(salespersonId, cancellationToken);
            if (salesperson != null)
            {
                salespeople[salespersonId] = salesperson;
            }
        }

        // Load approvals for all requests
        var approvalsByRequest = new Dictionary<Guid, List<Approval>>();
        foreach (var request in requests)
        {
            var approvals = await _approvalRepository.GetByDiscountRequestIdAsync(request.Id, cancellationToken);
            approvalsByRequest[request.Id] = approvals.ToList();
        }

        // Build summaries
        foreach (var request in requests)
        {
            var customer = customers.GetValueOrDefault(request.CustomerId);
            var salesperson = salespeople.GetValueOrDefault(request.SalespersonId);
            var approvals = approvalsByRequest.GetValueOrDefault(request.Id, new List<Approval>());

            var latestApproval = approvals.OrderByDescending(a => a.DecisionDateTime).FirstOrDefault();

            var summary = new DiscountRequestSummary
            {
                Id = request.Id,
                CustomerName = customer?.Name ?? "Unknown",
                SalespersonName = salesperson?.Name ?? "Unknown",
                RequestedDiscountPercentage = request.RequestedDiscountPercentage,
                EstimatedMarginPercentage = request.EstimatedMarginPercentage,
                RiskScore = request.RiskScore,
                RiskLevel = GetRiskLevel(request.RiskScore),
                Status = request.Status,
                StatusText = GetStatusText(request.Status),
                CreatedAt = request.CreatedAt,
                DecisionDateTime = latestApproval?.DecisionDateTime,
                SlaTimeInSeconds = latestApproval?.SlaTimeInSeconds,
                SlaTimeFormatted = latestApproval != null ? FormatSlaTime(latestApproval.SlaTimeInSeconds) : null,
                WasAutoApproved = request.Status == DiscountRequestStatus.AutoApprovedByAI,
                ApprovalSource = latestApproval?.Source,
                ApproverName = latestApproval != null ? await GetApproverNameAsync(latestApproval, cancellationToken) : null,
                Decision = latestApproval?.Decision,
                ItemCount = request.Items.Count,
                TotalValue = request.Items.Sum(i => i.GetTotalFinalPrice().Value)
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    /// <summary>
    /// Gets approver name (or "AI" for AI approvals)
    /// </summary>
    private async Task<string?> GetApproverNameAsync(Approval approval, CancellationToken cancellationToken)
    {
        if (approval.Source == Domain.Enums.ApprovalSource.AI)
            return "AI";

        if (approval.ApproverId.HasValue)
        {
            var approver = await _userRepository.GetByIdAsync(approval.ApproverId.Value, cancellationToken);
            return approver?.Name;
        }

        return null;
    }

    /// <summary>
    /// Calculates comprehensive statistics
    /// </summary>
    private async Task<HistoryStatistics> CalculateStatisticsAsync(
        List<DiscountRequest> requests,
        CancellationToken cancellationToken)
    {
        if (!requests.Any())
        {
            return new HistoryStatistics();
        }

        var totalRequests = requests.Count;
        var approvedRequests = requests.Count(r => r.Status == DiscountRequestStatus.Approved || 
                                                   r.Status == DiscountRequestStatus.AutoApprovedByAI);
        var rejectedRequests = requests.Count(r => r.Status == DiscountRequestStatus.Rejected);
        var pendingRequests = requests.Count(r => r.Status == DiscountRequestStatus.UnderAnalysis);
        var autoApprovedRequests = requests.Count(r => r.Status == DiscountRequestStatus.AutoApprovedByAI);

        var autoApprovalRate = totalRequests > 0 ? (decimal)autoApprovedRequests / totalRequests * 100 : 0;

        var avgDiscount = requests.Average(r => r.RequestedDiscountPercentage);
        var avgMargin = requests.Where(r => r.EstimatedMarginPercentage.HasValue)
            .Select(r => r.EstimatedMarginPercentage!.Value)
            .DefaultIfEmpty(0)
            .Average();
        var avgRiskScore = requests.Where(r => r.RiskScore.HasValue)
            .Select(r => r.RiskScore!.Value)
            .DefaultIfEmpty(0)
            .Average();

        // Calculate average SLA
        var allApprovals = new List<Approval>();
        foreach (var request in requests)
        {
            var approvals = await _approvalRepository.GetByDiscountRequestIdAsync(request.Id, cancellationToken);
            allApprovals.AddRange(approvals);
        }

        var avgSla = allApprovals.Any() ? (int)allApprovals.Average(a => a.SlaTimeInSeconds) : 0;

        // Human vs AI comparison
        var humanApprovals = allApprovals.Where(a => a.Source == Domain.Enums.ApprovalSource.Human).ToList();
        var aiApprovals = allApprovals.Where(a => a.Source == Domain.Enums.ApprovalSource.AI).ToList();

        var humanVsAI = new HumanVsAIComparison
        {
            HumanApprovals = humanApprovals.Count,
            AIApprovals = aiApprovals.Count,
            HumanApprovalRate = humanApprovals.Count > 0 
                ? (decimal)humanApprovals.Count(a => a.Decision == ApprovalDecision.Approve) / humanApprovals.Count * 100 
                : 0,
            AIApprovalRate = aiApprovals.Count > 0 
                ? (decimal)aiApprovals.Count(a => a.Decision == ApprovalDecision.Approve) / aiApprovals.Count * 100 
                : 0,
            AverageHumanSlaSeconds = humanApprovals.Any() ? (int)humanApprovals.Average(a => a.SlaTimeInSeconds) : 0,
            AverageAISlaSeconds = aiApprovals.Any() ? (int)aiApprovals.Average(a => a.SlaTimeInSeconds) : 0
        };

        humanVsAI.AverageHumanSlaFormatted = FormatSlaTime(humanVsAI.AverageHumanSlaSeconds);
        humanVsAI.AverageAISlaFormatted = FormatSlaTime(humanVsAI.AverageAISlaSeconds);

        // Get risk scores for human vs AI
        var humanApprovalRequestIds = humanApprovals.Select(a => a.DiscountRequestId).ToHashSet();
        var aiApprovalRequestIds = aiApprovals.Select(a => a.DiscountRequestId).ToHashSet();

        var humanApprovedRequests = requests.Where(r => humanApprovalRequestIds.Contains(r.Id) && r.RiskScore.HasValue).ToList();
        var aiApprovedRequests = requests.Where(r => aiApprovalRequestIds.Contains(r.Id) && r.RiskScore.HasValue).ToList();

        humanVsAI.AverageHumanRiskScore = humanApprovedRequests.Any() 
            ? humanApprovedRequests.Average(r => r.RiskScore!.Value) 
            : 0;
        humanVsAI.AverageAIRiskScore = aiApprovedRequests.Any() 
            ? aiApprovedRequests.Average(r => r.RiskScore!.Value) 
            : 0;

        // Risk distribution
        var requestsWithRisk = requests.Where(r => r.RiskScore.HasValue).ToList();
        var lowRisk = requestsWithRisk.Count(r => r.RiskScore < 30);
        var mediumRisk = requestsWithRisk.Count(r => r.RiskScore >= 30 && r.RiskScore < 60);
        var highRisk = requestsWithRisk.Count(r => r.RiskScore >= 60 && r.RiskScore < 80);
        var veryHighRisk = requestsWithRisk.Count(r => r.RiskScore >= 80);

        var totalWithRisk = requestsWithRisk.Count;

        var riskDistribution = new RiskDistribution
        {
            LowRisk = lowRisk,
            MediumRisk = mediumRisk,
            HighRisk = highRisk,
            VeryHighRisk = veryHighRisk,
            LowRiskPercentage = totalWithRisk > 0 ? (decimal)lowRisk / totalWithRisk * 100 : 0,
            MediumRiskPercentage = totalWithRisk > 0 ? (decimal)mediumRisk / totalWithRisk * 100 : 0,
            HighRiskPercentage = totalWithRisk > 0 ? (decimal)highRisk / totalWithRisk * 100 : 0,
            VeryHighRiskPercentage = totalWithRisk > 0 ? (decimal)veryHighRisk / totalWithRisk * 100 : 0
        };

        return new HistoryStatistics
        {
            TotalRequests = totalRequests,
            ApprovedRequests = approvedRequests,
            RejectedRequests = rejectedRequests,
            PendingRequests = pendingRequests,
            AutoApprovedRequests = autoApprovedRequests,
            AutoApprovalRate = autoApprovalRate,
            AverageDiscountPercentage = avgDiscount,
            AverageMarginPercentage = avgMargin,
            AverageRiskScore = avgRiskScore,
            AverageSlaTimeInSeconds = avgSla,
            AverageSlaTimeFormatted = FormatSlaTime(avgSla),
            HumanVsAI = humanVsAI,
            RiskDistribution = riskDistribution
        };
    }

    private string GetRiskLevel(decimal? riskScore)
    {
        if (!riskScore.HasValue) return "Unknown";
        if (riskScore < 30) return "Low";
        if (riskScore < 60) return "Medium";
        if (riskScore < 80) return "High";
        return "VeryHigh";
    }

    private string GetStatusText(DiscountRequestStatus status)
    {
        return status switch
        {
            DiscountRequestStatus.UnderAnalysis => "Under Analysis",
            DiscountRequestStatus.Approved => "Approved",
            DiscountRequestStatus.Rejected => "Rejected",
            DiscountRequestStatus.AdjustmentRequested => "Adjustment Requested",
            DiscountRequestStatus.AutoApprovedByAI => "Auto-Approved by AI",
            _ => status.ToString()
        };
    }

    private string FormatSlaTime(int slaTimeInSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(slaTimeInSeconds);

        if (timeSpan.TotalDays >= 1)
            return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
        
        if (timeSpan.TotalMinutes >= 1)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
        
        return $"{timeSpan.Seconds}s";
    }
}
