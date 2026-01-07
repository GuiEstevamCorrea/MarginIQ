using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Company entity
/// Company is the tenant entity, so no CompanyId filtering needed
/// </summary>
public class CompanyRepository : BaseRepository<Company>, ICompanyRepository
{
    public CompanyRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Company>> GetActiveCompaniesAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.Status == CompanyStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
