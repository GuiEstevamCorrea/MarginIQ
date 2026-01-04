using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// Repository interface for BusinessRule entity operations
/// </summary>
public interface IBusinessRuleRepository
{
    /// <summary>
    /// Gets a business rule by its unique identifier
    /// </summary>
    /// <param name="id">Business rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business rule if found, null otherwise</returns>
    Task<BusinessRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all business rules for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business rules</returns>
    Task<IEnumerable<BusinessRule>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active business rules for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active business rules ordered by priority</returns>
    Task<IEnumerable<BusinessRule>> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets business rules by type for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="type">Rule type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business rules of the specified type</returns>
    Task<IEnumerable<BusinessRule>> GetByTypeAsync(Guid companyId, RuleType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets business rules by scope for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="scope">Rule scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business rules with the specified scope</returns>
    Task<IEnumerable<BusinessRule>> GetByScopeAsync(Guid companyId, RuleScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active business rules that apply to a specific product
    /// Includes product-specific, category-specific, and global rules
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="productId">Product ID</param>
    /// <param name="categoryName">Product category name (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applicable business rules ordered by priority</returns>
    Task<IEnumerable<BusinessRule>> GetApplicableToProductAsync(Guid companyId, Guid productId, string? categoryName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active business rules that apply to a specific customer
    /// Includes customer-specific and global rules
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="customerId">Customer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applicable business rules ordered by priority</returns>
    Task<IEnumerable<BusinessRule>> GetApplicableToCustomerAsync(Guid companyId, Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active business rules that apply to a specific user role
    /// Includes role-specific and global rules
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="userRole">User role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of applicable business rules ordered by priority</returns>
    Task<IEnumerable<BusinessRule>> GetApplicableToUserRoleAsync(Guid companyId, UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all global business rules for a company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of global business rules</returns>
    Task<IEnumerable<BusinessRule>> GetGlobalRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets minimum margin rules for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of minimum margin rules</returns>
    Task<IEnumerable<BusinessRule>> GetMinimumMarginRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discount limit rules for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discount limit rules</returns>
    Task<IEnumerable<BusinessRule>> GetDiscountLimitRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets auto-approval rules for a specific company
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of auto-approval rules</returns>
    Task<IEnumerable<BusinessRule>> GetAutoApprovalRulesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets business rules by target entity ID
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="targetEntityId">Target entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business rules for the target entity</returns>
    Task<IEnumerable<BusinessRule>> GetByTargetEntityIdAsync(Guid companyId, Guid targetEntityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new business rule to the repository
    /// </summary>
    /// <param name="businessRule">Business rule to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(BusinessRule businessRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing business rule
    /// </summary>
    /// <param name="businessRule">Business rule to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(BusinessRule businessRule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a business rule
    /// </summary>
    /// <param name="id">Business rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a business rule exists by ID
    /// </summary>
    /// <param name="id">Business rule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if business rule exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a business rule with the given name already exists in a company
    /// </summary>
    /// <param name="name">Rule name</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="excludeId">Optional rule ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a rule with the name exists in the company, false otherwise</returns>
    Task<bool> ExistsByNameAsync(string name, Guid companyId, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
