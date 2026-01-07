using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Customer entity
/// All queries filter by CompanyId for multi-tenant isolation
/// </summary>
public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId && c.Status == CustomerStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByClassificationAsync(Guid companyId, CustomerClassification classification, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId && c.Classification == classification)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetBySegmentAsync(Guid companyId, string segment, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId && c.Segment == segment)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByStatusAsync(Guid companyId, CustomerStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId && c.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByExternalIdAsync(Guid companyId, string externalSystemId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ExternalSystemId == externalSystemId, cancellationToken);
    }

    public async Task<bool> ExistsByExternalIdAsync(Guid companyId, string externalSystemId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.CompanyId == companyId && c.ExternalSystemId == externalSystemId);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.CompanyId == companyId && c.Name.Contains(searchTerm))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(c => c.CompanyId == companyId && c.Status == CustomerStatus.Active, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.CompanyId == companyId && c.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
