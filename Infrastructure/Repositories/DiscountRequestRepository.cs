using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for DiscountRequest entity
/// All queries filter by CompanyId for multi-tenant isolation
/// Includes navigation properties (Customer, Salesperson, Items)
/// </summary>
public class DiscountRequestRepository : BaseRepository<DiscountRequest>, IDiscountRequestRepository
{
    public DiscountRequestRepository(MarginIQDbContext context) : base(context)
    {
    }

    public override async Task<DiscountRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .FirstOrDefaultAsync(dr => dr.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByStatusAsync(Guid companyId, DiscountRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.Status == status)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetPendingByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.UnderAnalysis)
            .OrderBy(dr => dr.CreatedAt) // Oldest first for pending
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetBySalespersonAsync(Guid companyId, Guid salespersonId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.SalespersonId == salespersonId)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetBySalespersonIdAsync(Guid companyId, Guid salespersonId, CancellationToken cancellationToken = default)
    {
        return await GetBySalespersonAsync(companyId, salespersonId, cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByCustomerIdAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await GetByCustomerAsync(companyId, customerId, cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetAutoApprovedByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.AutoApprovedByAI)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetHighRiskByCompanyIdAsync(Guid companyId, decimal threshold = 70, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.RiskScore.HasValue && dr.RiskScore.Value >= threshold)
            .OrderByDescending(dr => dr.RiskScore)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByProductIdAsync(Guid companyId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId 
                && dr.Items.Any(i => i.ProductId == productId))
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IDictionary<string, int>> GetStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var statistics = new Dictionary<string, int>
        {
            ["Total"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId, cancellationToken),
            ["Pending"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.UnderAnalysis, cancellationToken),
            ["Approved"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.Approved, cancellationToken),
            ["Rejected"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.Rejected, cancellationToken),
            ["AutoApproved"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.AutoApprovedByAI, cancellationToken),
            ["AdjustmentRequested"] = await DbSet.CountAsync(dr => dr.CompanyId == companyId && dr.Status == DiscountRequestStatus.AdjustmentRequested, cancellationToken)
        };

        return statistics;
    }

    public async Task<IEnumerable<DiscountRequest>> GetByCustomerAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.CustomerId == customerId)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId && dr.CreatedAt >= startDate && dr.CreatedAt <= endDate)
            .OrderByDescending(dr => dr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscountRequest>> GetApprovedByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dr => dr.Customer)
            .Include(dr => dr.Salesperson)
            .Where(dr => dr.CompanyId == companyId 
                && (dr.Status == DiscountRequestStatus.Approved || dr.Status == DiscountRequestStatus.AutoApprovedByAI)
                && dr.DecisionAt.HasValue
                && dr.DecisionAt.Value >= startDate 
                && dr.DecisionAt.Value <= endDate)
            .OrderByDescending(dr => dr.DecisionAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(dr => dr.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(Guid companyId, DiscountRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(dr => dr.CompanyId == companyId && dr.Status == status, cancellationToken);
    }

    public async Task<decimal> GetAverageDiscountPercentageAsync(Guid companyId, DateTime? startDate = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(dr => dr.CompanyId == companyId 
            && (dr.Status == DiscountRequestStatus.Approved || dr.Status == DiscountRequestStatus.AutoApprovedByAI));

        if (startDate.HasValue)
        {
            query = query.Where(dr => dr.CreatedAt >= startDate.Value);
        }

        if (!await query.AnyAsync(cancellationToken))
        {
            return 0;
        }

        return await query.AverageAsync(dr => dr.RequestedDiscountPercentage, cancellationToken);
    }
}
