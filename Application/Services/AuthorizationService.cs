using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using MarginIQ.Application.Ports;

namespace Application.Services;

/// <summary>
/// Concrete implementation of `IAuthorizationService`.
/// Uses repositories to evaluate roles, company/user status and resource ownership.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IDiscountRequestRepository _discountRequestRepository;

    public AuthorizationService(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IDiscountRequestRepository discountRequestRepository)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _discountRequestRepository = discountRequestRepository;
    }

    public async Task<bool> HasRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.Role == role;
    }

    public async Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<UserRole> roles, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && roles.Contains(user.Role);
    }

    public async Task<bool> HasAllRolesAsync(Guid userId, IEnumerable<UserRole> roles, CancellationToken cancellationToken = default)
    {
        // In this system a user has a single role, so "all roles" is true only when the provided set
        // contains exactly the user's role (or is a superset).
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && roles.All(r => r == user.Role) && roles.Any();
    }

    public async Task<bool> BelongsToCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.CompanyId == companyId;
    }

    public async Task<bool> CanApproveDiscountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;
        if (!user.IsActive()) return false;
        return user.CanApproveDiscounts();
    }

    public async Task<bool> CanConfigureAIAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && user.IsAdmin();
    }

    public async Task<bool> CanManageBusinessRulesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && (user.IsManager() || user.IsAdmin());
    }

    public async Task<bool> CanImportDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && user.IsAdmin();
    }

    public async Task<bool> CanExportDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && (user.IsManager() || user.IsAdmin());
    }

    public async Task<bool> CanViewAuditLogsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && (user.IsManager() || user.IsAdmin());
    }

    public async Task<bool> CanManageUsersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive() && user.IsAdmin();
    }

    public async Task<bool> CanViewDiscountRequestAsync(Guid userId, Guid discountRequestId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive()) return false;

        var request = await _discountRequestRepository.GetByIdAsync(discountRequestId, cancellationToken);
        if (request == null) return false;

        if (user.IsAdmin() || user.IsManager())
            return request.CompanyId == user.CompanyId;

        // Salesperson: only their own requests
        if (user.IsSalesperson())
            return request.SalespersonId == userId && request.CompanyId == user.CompanyId;

        return false;
    }

    public async Task<bool> CanEditDiscountRequestAsync(Guid userId, Guid discountRequestId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive()) return false;

        var request = await _discountRequestRepository.GetByIdAsync(discountRequestId, cancellationToken);
        if (request == null) return false;

        // Salesperson: only their own requests in Draft (UnderAnalysis) or AdjustmentRequested
        if (user.IsSalesperson())
            return request.SalespersonId == userId &&
                   (request.Status == DiscountRequestStatus.UnderAnalysis || request.Status == DiscountRequestStatus.AdjustmentRequested) &&
                   request.CompanyId == user.CompanyId;

        // Managers/Admins cannot edit requests (they approve/reject)
        return false;
    }

    public async Task<UserPermissions> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return new UserPermissions { UserId = userId };

        var perms = UserPermissions.ForRole(user.Role);
        perms.UserId = user.Id;
        perms.UserName = user.Name;
        perms.Email = user.Email;
        perms.Role = user.Role;
        perms.CompanyId = user.CompanyId;

        var company = await _companyRepository.GetByIdAsync(user.CompanyId, cancellationToken);
        if (company != null)
        {
            perms.CompanyName = company.Name;
            perms.IsCompanyActive = company.IsActive();
        }

        perms.IsActive = user.IsActive();

        return perms;
    }

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user != null && user.IsActive();
    }

    public async Task<bool> IsCompanyActiveAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
        return company != null && company.IsActive();
    }
}
