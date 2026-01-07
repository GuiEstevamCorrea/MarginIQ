using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Approval entity
/// All queries include navigation properties where needed
/// </summary>
public class ApprovalRepository : BaseRepository<Approval>, IApprovalRepository
{
    public ApprovalRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Approval>> GetByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Approver)
            .Where(a => a.DiscountRequestId == discountRequestId)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<Approval?> GetLatestByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.Approver)
            .Where(a => a.DiscountRequestId == discountRequestId)
            .OrderByDescending(a => a.DecisionDateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByApproverIdAsync(Guid approverId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
                .ThenInclude(dr => dr!.Customer)
            .Include(a => a.Approver)
            .Where(a => a.ApproverId == approverId)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetBySourceAsync(ApprovalSource source, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.Source == source)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.Decision == decision)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.DecisionDateTime >= startDate && a.DecisionDateTime <= endDate)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAverageSlaTimeInSecondsAsync(ApprovalSource? source = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (source.HasValue)
        {
            query = query.Where(a => a.Source == source.Value);
        }

        if (!await query.AnyAsync(cancellationToken))
        {
            return 0;
        }

        return (int)await query.AverageAsync(a => a.SlaTimeInSeconds, cancellationToken);
    }

    public async Task<int> CountBySourceAsync(ApprovalSource source, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(a => a.Source == source, cancellationToken);
    }

    public async Task<int> CountByDecisionAsync(ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(a => a.Decision == decision, cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetAIApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.Source == ApprovalSource.AI)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetExceededSlaAsync(int slaThresholdInSeconds, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.SlaTimeInSeconds > slaThresholdInSeconds)
            .OrderByDescending(a => a.SlaTimeInSeconds)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Approval>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.DiscountRequest)
            .Include(a => a.Approver)
            .Where(a => a.DiscountRequest!.CompanyId == companyId)
            .OrderByDescending(a => a.DecisionDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IDictionary<string, object>> GetStatisticsAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(a => a.DiscountRequest).AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(a => a.DiscountRequest!.CompanyId == companyId.Value);
        }

        var statistics = new Dictionary<string, object>
        {
            ["TotalApprovals"] = await query.CountAsync(cancellationToken),
            ["ApprovedCount"] = await query.CountAsync(a => a.Decision == ApprovalDecision.Approve, cancellationToken),
            ["RejectedCount"] = await query.CountAsync(a => a.Decision == ApprovalDecision.Reject, cancellationToken),
            ["AICount"] = await query.CountAsync(a => a.Source == ApprovalSource.AI, cancellationToken),
            ["HumanCount"] = await query.CountAsync(a => a.Source == ApprovalSource.Human, cancellationToken),
            ["AverageSlaTime"] = await query.AnyAsync(cancellationToken) 
                ? await query.AverageAsync(a => a.SlaTimeInSeconds, cancellationToken) 
                : 0.0
        };

        return statistics;
    }

    public async Task<double> GetAverageSlaTimeAsync(Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Include(a => a.DiscountRequest).AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(a => a.DiscountRequest!.CompanyId == companyId.Value);
        }

        if (!await query.AnyAsync(cancellationToken))
        {
            return 0;
        }

        return await query.AverageAsync(a => (double)a.SlaTimeInSeconds, cancellationToken);
    }
}
