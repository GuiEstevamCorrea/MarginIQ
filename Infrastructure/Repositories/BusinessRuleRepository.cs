using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for BusinessRule entity
/// All queries filter by CompanyId for multi-tenant isolation
/// </summary>
public class BusinessRuleRepository : BaseRepository<BusinessRule>, IBusinessRuleRepository
{
    public BusinessRuleRepository(MarginIQDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<BusinessRule>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByTypeAsync(Guid companyId, RuleType type, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.Type == type && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByScopeAsync(Guid companyId, RuleScope scope, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.Scope == scope && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByTypeAndScopeAsync(Guid companyId, RuleType type, RuleScope scope, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.Type == type && br.Scope == scope && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByTargetEntityAsync(Guid companyId, Guid targetEntityId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.TargetEntityId == targetEntityId && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByTargetIdentifierAsync(Guid companyId, string targetIdentifier, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.TargetIdentifier == targetIdentifier && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(br => br.CompanyId == companyId, cancellationToken);
    }

    public async Task<int> CountActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(br => br.CompanyId == companyId && br.IsActive, cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetApplicableToProductAsync(Guid companyId, Guid productId, string? categoryName = null, CancellationToken cancellationToken = default)
    {
        var rules = new List<BusinessRule>();

        // Product-specific rules
        var productRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.Product 
                && br.TargetEntityId == productId)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(productRules);

        // Category-specific rules (if category is provided)
        if (!string.IsNullOrEmpty(categoryName))
        {
            var categoryRules = await DbSet
                .Where(br => br.CompanyId == companyId 
                    && br.IsActive 
                    && br.Scope == RuleScope.Category 
                    && br.TargetIdentifier == categoryName)
                .OrderBy(br => br.Priority)
                .ToListAsync(cancellationToken);
            rules.AddRange(categoryRules);
        }

        // Global rules
        var globalRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.Global)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(globalRules);

        return rules.OrderBy(r => r.Priority).ToList();
    }

    public async Task<IEnumerable<BusinessRule>> GetApplicableToCustomerAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var rules = new List<BusinessRule>();

        // Customer-specific rules
        var customerRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.Customer 
                && br.TargetEntityId == customerId)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(customerRules);

        // Global rules
        var globalRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.Global)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(globalRules);

        return rules.OrderBy(r => r.Priority).ToList();
    }

    public async Task<IEnumerable<BusinessRule>> GetApplicableToUserRoleAsync(Guid companyId, UserRole userRole, CancellationToken cancellationToken = default)
    {
        var rules = new List<BusinessRule>();

        // Role-specific rules
        var roleRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.UserRole 
                && br.TargetIdentifier == userRole.ToString())
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(roleRules);

        // Global rules
        var globalRules = await DbSet
            .Where(br => br.CompanyId == companyId 
                && br.IsActive 
                && br.Scope == RuleScope.Global)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
        rules.AddRange(globalRules);

        return rules.OrderBy(r => r.Priority).ToList();
    }

    public async Task<IEnumerable<BusinessRule>> GetGlobalRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.IsActive && br.Scope == RuleScope.Global)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetMinimumMarginRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.IsActive && br.Type == RuleType.MinimumMargin)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetDiscountLimitRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.IsActive && br.Type == RuleType.DiscountLimit)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetAutoApprovalRulesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.IsActive && br.Type == RuleType.AutoApproval)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<BusinessRule>> GetByTargetEntityIdAsync(Guid companyId, Guid targetEntityId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(br => br.CompanyId == companyId && br.TargetEntityId == targetEntityId && br.IsActive)
            .OrderBy(br => br.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await DbSet.FindAsync(new object[] { id }, cancellationToken);
        if (rule != null)
        {
            DbSet.Remove(rule);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(br => br.CompanyId == companyId && br.Name == name);
        
        if (excludeId.HasValue)
        {
            query = query.Where(br => br.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
