using Domain.Enums;

namespace MarginIQ.Application.Ports;

/// <summary>
/// Authorization service port for role-based access control and permission checking.
/// Implements security requirements from Projeto.md section 8.2.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user has a specific role.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Required role</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has the role</returns>
    Task<bool> HasRoleAsync(
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified roles.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">Required roles (any match)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has at least one role</returns>
    Task<bool> HasAnyRoleAsync(
        Guid userId,
        IEnumerable<UserRole> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has all of the specified roles.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roles">Required roles (all must match)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has all roles</returns>
    Task<bool> HasAllRolesAsync(
        Guid userId,
        IEnumerable<UserRole> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user belongs to a specific company (multi-tenant isolation).
    /// CRITICAL: Always verify company ownership before data access.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user belongs to company</returns>
    Task<bool> BelongsToCompanyAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can approve discount requests.
    /// Only Managers and Admins can approve.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can approve</returns>
    Task<bool> CanApproveDiscountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can configure AI governance settings.
    /// Only Admins can configure AI.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can configure AI</returns>
    Task<bool> CanConfigureAIAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can manage business rules.
    /// Only Managers and Admins can manage rules.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can manage rules</returns>
    Task<bool> CanManageBusinessRulesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can import data from external systems.
    /// Only Admins can import data.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can import data</returns>
    Task<bool> CanImportDataAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can export data.
    /// Only Managers and Admins can export.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can export data</returns>
    Task<bool> CanExportDataAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can view audit logs.
    /// Only Managers and Admins can view audit logs.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can view audit logs</returns>
    Task<bool> CanViewAuditLogsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can manage other users.
    /// Only Admins can manage users.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can manage users</returns>
    Task<bool> CanManageUsersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can view a specific discount request.
    /// - Salesperson: Only their own requests
    /// - Manager/Admin: All requests in their company
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can view the request</returns>
    Task<bool> CanViewDiscountRequestAsync(
        Guid userId,
        Guid discountRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user can edit a specific discount request.
    /// - Salesperson: Only their own requests in "Draft" or "AdjustmentRequired" status
    /// - Manager/Admin: Cannot edit (can only approve/reject)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="discountRequestId">Discount request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user can edit the request</returns>
    Task<bool> CanEditDiscountRequestAsync(
        Guid userId,
        Guid discountRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user.
    /// Used for UI to show/hide features.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permissions</returns>
    Task<UserPermissions> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a user is active and not blocked.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is active</returns>
    Task<bool> IsUserActiveAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a company is active and not suspended.
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if company is active</returns>
    Task<bool> IsCompanyActiveAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Complete set of permissions for a user.
/// Used for authorization decisions and UI rendering.
/// </summary>
public class UserPermissions
{
    // Identity
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    
    // Core permissions
    public bool CanCreateDiscountRequests { get; set; }
    public bool CanApproveDiscounts { get; set; }
    public bool CanRejectDiscounts { get; set; }
    public bool CanRequestAdjustments { get; set; }
    
    // AI permissions
    public bool CanConfigureAI { get; set; }
    public bool CanViewAIDecisions { get; set; }
    public bool CanOverrideAI { get; set; }
    
    // Data permissions
    public bool CanImportData { get; set; }
    public bool CanExportData { get; set; }
    public bool CanViewAuditLogs { get; set; }
    
    // Management permissions
    public bool CanManageUsers { get; set; }
    public bool CanManageBusinessRules { get; set; }
    public bool CanManageCustomers { get; set; }
    public bool CanManageProducts { get; set; }
    
    // Reporting permissions
    public bool CanViewAllRequests { get; set; }
    public bool CanViewOwnRequestsOnly { get; set; }
    public bool CanViewReports { get; set; }
    public bool CanViewAnalytics { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsCompanyActive { get; set; }
    
    /// <summary>
    /// Factory method to create permissions based on role.
    /// </summary>
    public static UserPermissions ForRole(UserRole role)
    {
        return role switch
        {
            UserRole.Salesperson => new UserPermissions
            {
                CanCreateDiscountRequests = true,
                CanViewOwnRequestsOnly = true,
                CanViewAIDecisions = true,
                // Limited permissions
                CanApproveDiscounts = false,
                CanConfigureAI = false,
                CanImportData = false,
                CanExportData = false,
                CanManageUsers = false,
                CanManageBusinessRules = false,
                CanViewAuditLogs = false,
                CanViewAllRequests = false
            },
            
            UserRole.Manager => new UserPermissions
            {
                CanCreateDiscountRequests = true,
                CanApproveDiscounts = true,
                CanRejectDiscounts = true,
                CanRequestAdjustments = true,
                CanViewAllRequests = true,
                CanViewAIDecisions = true,
                CanOverrideAI = true,
                CanExportData = true,
                CanViewAuditLogs = true,
                CanViewReports = true,
                CanViewAnalytics = true,
                CanManageBusinessRules = true,
                CanManageCustomers = true,
                CanManageProducts = true,
                // Still limited
                CanConfigureAI = false,
                CanImportData = false,
                CanManageUsers = false
            },
            
            UserRole.Admin => new UserPermissions
            {
                // Full permissions
                CanCreateDiscountRequests = true,
                CanApproveDiscounts = true,
                CanRejectDiscounts = true,
                CanRequestAdjustments = true,
                CanConfigureAI = true,
                CanViewAIDecisions = true,
                CanOverrideAI = true,
                CanImportData = true,
                CanExportData = true,
                CanViewAuditLogs = true,
                CanManageUsers = true,
                CanManageBusinessRules = true,
                CanManageCustomers = true,
                CanManageProducts = true,
                CanViewAllRequests = true,
                CanViewReports = true,
                CanViewAnalytics = true
            },
            
            _ => new UserPermissions() // No permissions by default
        };
    }
}
