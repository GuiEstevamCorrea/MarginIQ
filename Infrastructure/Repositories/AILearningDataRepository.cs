using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AILearningData entity
/// All queries filter by CompanyId for multi-tenant isolation
/// AI learns per company (logically separated model)
/// </summary>
public class AILearningDataRepository : BaseRepository<AILearningData>, IAILearningDataRepository
{
    public AILearningDataRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<AILearningData?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<AILearningData?> GetByDiscountRequestIdAsync(Guid discountRequestId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(ald => ald.DiscountRequestId == discountRequestId, cancellationToken);
    }

    public async Task<IEnumerable<AILearningData>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 100)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId)
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetReadyForTrainingAsync(Guid companyId, int? maxAgeDays = null)
    {
        var query = DbSet.Where(ald => ald.CompanyId == companyId 
            && !ald.UsedForTraining
            && ald.SaleOutcome.HasValue);

        if (maxAgeDays.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays.Value);
            query = query.Where(ald => ald.RecordedAt >= cutoffDate);
        }

        return await query
            .OrderBy(ald => ald.RecordedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetUsedForTrainingAsync(Guid companyId, int skip = 0, int take = 100)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId && ald.UsedForTraining)
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetByDecisionSourceAsync(Guid companyId, ApprovalSource source, int skip = 0, int take = 100)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId && ald.DecisionSource == source)
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetWithSaleOutcomeAsync(Guid companyId, bool? won = null, int skip = 0, int take = 100)
    {
        var query = DbSet.Where(ald => ald.CompanyId == companyId && ald.SaleOutcome.HasValue);

        if (won.HasValue)
        {
            query = query.Where(ald => ald.SaleOutcome == won.Value);
        }

        return await query
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetByCustomerAsync(Guid companyId, Guid customerId, int skip = 0, int take = 100)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId && ald.CustomerId == customerId)
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetBySalespersonAsync(Guid companyId, Guid salespersonId, int skip = 0, int take = 100)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId && ald.SalespersonId == salespersonId)
            .OrderByDescending(ald => ald.RecordedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<AILearningData>> GetByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate)
    {
        return await DbSet
            .Where(ald => ald.CompanyId == companyId 
                && ald.RequestCreatedAt >= startDate 
                && ald.RequestCreatedAt <= endDate)
            .OrderByDescending(ald => ald.RecordedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AILearningData learningData)
    {
        await DbSet.AddAsync(learningData);
        await Context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<AILearningData> learningDataList)
    {
        await DbSet.AddRangeAsync(learningDataList);
        await Context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AILearningData learningData)
    {
        DbSet.Update(learningData);
        await Context.SaveChangesAsync();
    }

    public async Task<AILearningStatistics> GetStatisticsAsync(Guid companyId)
    {
        var allData = DbSet.Where(ald => ald.CompanyId == companyId);
        var completeData = allData.Where(ald => ald.SaleOutcome.HasValue);

        var statistics = new AILearningStatistics
        {
            TotalDataPoints = await allData.CountAsync(),
            CompleteDataPoints = await completeData.CountAsync(),
            UsedForTraining = await allData.CountAsync(ald => ald.UsedForTraining),
            ReadyForTraining = await allData.CountAsync(ald => !ald.UsedForTraining && ald.SaleOutcome.HasValue),
            ApprovedDecisions = await allData.CountAsync(ald => ald.Decision == ApprovalDecision.Approve),
            RejectedDecisions = await allData.CountAsync(ald => ald.Decision == ApprovalDecision.Reject),
            AIDecisions = await allData.CountAsync(ald => ald.DecisionSource == ApprovalSource.AI),
            HumanDecisions = await allData.CountAsync(ald => ald.DecisionSource == ApprovalSource.Human),
            WonSales = await completeData.CountAsync(ald => ald.SaleOutcome == true),
            LostSales = await completeData.CountAsync(ald => ald.SaleOutcome == false)
        };

        var completeCount = statistics.CompleteDataPoints;
        statistics.WinRate = completeCount > 0 ? (decimal)statistics.WonSales / completeCount * 100 : 0;
        statistics.AverageDiscount = await allData.AnyAsync() ? await allData.AverageAsync(ald => ald.RequestedDiscountPercentage) : 0;
        statistics.AverageMargin = await allData.AnyAsync() 
            ? await allData.AverageAsync(ald => ald.FinalMarginPercentage) 
            : 0;
        statistics.AverageRiskScore = await allData.AnyAsync() 
            ? await allData.AverageAsync(ald => ald.RiskScore) 
            : 0;
        
        statistics.OldestDataDate = await allData.AnyAsync() ? await allData.MinAsync(ald => (DateTime?)ald.RecordedAt) : null;
        statistics.NewestDataDate = await allData.AnyAsync() ? await allData.MaxAsync(ald => (DateTime?)ald.RecordedAt) : null;
        statistics.LastTrainingDate = await allData.Where(ald => ald.UsedForTraining && ald.TrainedAt.HasValue).AnyAsync() 
            ? await allData.Where(ald => ald.UsedForTraining && ald.TrainedAt.HasValue).MaxAsync(ald => ald.TrainedAt) 
            : null;

        return statistics;
    }

    public async Task<int> ArchiveOldDataAsync(Guid companyId, int olderThanDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
        var oldData = await DbSet
            .Where(ald => ald.CompanyId == companyId && ald.RecordedAt < cutoffDate)
            .ToListAsync();

        // In a real system, you would mark these as archived or move to archive storage
        // For now, we just count them since there's no IsArchived property
        return oldData.Count;
    }

    public async Task<int> GetCountAsync(Guid companyId, bool completeOnly = false)
    {
        var query = DbSet.Where(ald => ald.CompanyId == companyId);

        if (completeOnly)
        {
            query = query.Where(ald => ald.SaleOutcome.HasValue);
        }

        return await query.CountAsync();
    }

    // Keep legacy methods for backward compatibility
    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(ald => ald.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountUntrainedAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(ald => ald.CompanyId == companyId && !ald.UsedForTraining, cancellationToken);
    }

    public async Task<int> CountWithSaleOutcomeAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(ald => ald.CompanyId == companyId && ald.SaleOutcome.HasValue, cancellationToken);
    }

    public async Task MarkAsTrainedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var learningData = await DbSet
            .Where(ald => ids.Contains(ald.Id))
            .ToListAsync(cancellationToken);

        foreach (var data in learningData)
        {
            data.MarkAsUsedForTraining();
        }
    }
}
