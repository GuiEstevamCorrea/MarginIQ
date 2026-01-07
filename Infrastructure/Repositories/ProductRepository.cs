using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Product entity
/// All queries filter by CompanyId for multi-tenant isolation
/// </summary>
public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId && p.Status == ProductStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(Guid companyId, string category, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId && p.Category == category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByStatusAsync(Guid companyId, ProductStatus status, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId && p.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetBySkuAsync(string sku, Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Sku == sku, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.CompanyId == companyId && p.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySkuAsync(string sku, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(p => p.CompanyId == companyId && p.Sku == sku);
        
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(Guid companyId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId && p.Name.Contains(searchTerm))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.CompanyId == companyId && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(p => p.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(p => p.CompanyId == companyId && p.Status == ProductStatus.Active, cancellationToken);
    }
}
