using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity
/// All queries filter by CompanyId for multi-tenant isolation
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.CompanyId == companyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.CompanyId == companyId && u.Status == UserStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(Guid companyId, UserRole role, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.CompanyId == companyId && u.Role == role)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Guid companyId, string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.CompanyId == companyId && u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetApproversAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(u => u.CompanyId == companyId 
                && u.Status == UserStatus.Active
                && (u.Role == UserRole.Manager || u.Role == UserRole.Admin))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(u => u.CompanyId == companyId && u.Email == email);
        
        if (excludeId.HasValue)
        {
            query = query.Where(u => u.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(u => u.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(u => u.CompanyId == companyId && u.Status == UserStatus.Active, cancellationToken);
    }
}
