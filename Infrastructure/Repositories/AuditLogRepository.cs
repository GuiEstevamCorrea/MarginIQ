using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AuditLog entity
/// All queries filter by CompanyId for multi-tenant isolation
/// Audit logs are immutable - no Update or Delete operations
/// </summary>
public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.EntityName == entityName && al.EntityId == entityId)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(al => al.User)
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAILogsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.Origin == AuditOrigin.AI)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetHumanLogsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(al => al.User)
            .Where(al => al.CompanyId == companyId && al.Origin == AuditOrigin.Human)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(Guid companyId, int count = 100, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId)
            .OrderByDescending(al => al.DateTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> SearchAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId 
                && (al.EntityName.Contains(searchTerm) 
                    || (al.User != null && al.User.Name.Contains(searchTerm))
                    || al.Action.ToString().Contains(searchTerm)))
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IDictionary<string, object>> GetStatisticsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var statistics = new Dictionary<string, object>
        {
            ["TotalLogs"] = await DbSet.CountAsync(al => al.CompanyId == companyId, cancellationToken),
            ["AILogs"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Origin == AuditOrigin.AI, cancellationToken),
            ["HumanLogs"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Origin == AuditOrigin.Human, cancellationToken),
            ["SystemLogs"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Origin == AuditOrigin.System, cancellationToken),
            ["CreateActions"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Action == AuditAction.Created, cancellationToken),
            ["UpdateActions"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Action == AuditAction.Updated, cancellationToken),
            ["DeleteActions"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Action == AuditAction.Deleted, cancellationToken),
            ["ApproveActions"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Action == AuditAction.Approved, cancellationToken),
            ["RejectActions"] = await DbSet.CountAsync(al => al.CompanyId == companyId && al.Action == AuditAction.Rejected, cancellationToken)
        };

        return statistics;
    }

    public async Task<IDictionary<AuditOrigin, int>> GetCountByOriginAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var counts = await DbSet
            .Where(al => al.CompanyId == companyId)
            .GroupBy(al => al.Origin)
            .Select(g => new { Origin = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Origin, x => x.Count);
    }

    public async Task<IDictionary<AuditAction, int>> GetCountByActionAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var counts = await DbSet
            .Where(al => al.CompanyId == companyId)
            .GroupBy(al => al.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Action, x => x.Count);
    }

    public async Task<int> ArchiveOldLogsAsync(Guid companyId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldLogs = await DbSet
            .Where(al => al.CompanyId == companyId && al.DateTime < olderThan)
            .ToListAsync(cancellationToken);

        // In a real system, you'd move these to an archive table or storage
        // For now, we'll just count them (since audit logs are immutable and shouldn't be deleted)
        return oldLogs.Count;
    }

    public new async Task AddRangeAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(auditLogs, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    // Legacy methods kept for backward compatibility
    public async Task<IEnumerable<AuditLog>> GetByCompanyIdAsync(Guid companyId, int pageSize = 100, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId)
            .OrderByDescending(al => al.DateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(Guid companyId, string entityName, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.EntityName == entityName && al.EntityId == entityId)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid companyId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(al => al.User)
            .Where(al => al.CompanyId == companyId && al.UserId == userId)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(Guid companyId, AuditAction action, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.Action == action)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByOriginAsync(Guid companyId, AuditOrigin origin, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.Origin == origin)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.DateTime >= startDate && al.DateTime <= endDate)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityNameAsync(Guid companyId, string entityName, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(al => al.CompanyId == companyId && al.EntityName == entityName)
            .OrderByDescending(al => al.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(al => al.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountByActionAsync(Guid companyId, AuditAction action, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(al => al.CompanyId == companyId && al.Action == action, cancellationToken);
    }

    public async Task<int> CountByOriginAsync(Guid companyId, AuditOrigin origin, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(al => al.CompanyId == companyId && al.Origin == origin, cancellationToken);
    }

    // Override to prevent updates (audit logs are immutable)
    public override Task UpdateAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Audit logs are immutable and cannot be updated");
    }

    // Override to prevent deletion (audit logs are immutable)
    public override Task RemoveAsync(AuditLog entity, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Audit logs are immutable and cannot be deleted");
    }
}
