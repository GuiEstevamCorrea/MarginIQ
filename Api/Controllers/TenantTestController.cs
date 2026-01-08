using Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Debug controller to test multi-tenant authentication and authorization.
/// This controller is for development and testing purposes only.
/// In production, remove this controller and use the real business controllers.
/// </summary>
[ApiController]
[Route("api/debug/[controller]")]
[Authorize]
public class TenantTestController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantTestController> _logger;

    public TenantTestController(ITenantContext tenantContext, ILogger<TenantTestController> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets current tenant context information from JWT token.
    /// Requires valid authentication.
    /// </summary>
    /// <returns>Tenant context details</returns>
    [HttpGet("context")]
    public IActionResult GetTenantContext()
    {
        _logger.LogInformation("Tenant context requested by User {UserId} from Company {CompanyId}",
            _tenantContext.UserId, _tenantContext.CompanyId);

        return Ok(new
        {
            isAuthenticated = _tenantContext.IsAuthenticated,
            companyId = _tenantContext.CompanyId,
            companyName = _tenantContext.CompanyName,
            userId = _tenantContext.UserId,
            userName = _tenantContext.UserName,
            userEmail = _tenantContext.UserEmail,
            userRole = _tenantContext.UserRole,
            requestTimestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets all claims from the JWT token.
    /// Useful for debugging and understanding token structure.
    /// </summary>
    /// <returns>All JWT claims</returns>
    [HttpGet("claims")]
    public IActionResult GetAllClaims()
    {
        _logger.LogInformation("Claims requested by User {UserId} from Company {CompanyId}",
            _tenantContext.UserId, _tenantContext.CompanyId);

        return Ok(new
        {
            tenantInfo = new
            {
                companyId = _tenantContext.CompanyId,
                companyName = _tenantContext.CompanyName,
                userId = _tenantContext.UserId,
                userName = _tenantContext.UserName,
                userRole = _tenantContext.UserRole
            },
            allClaims = _tenantContext.AdditionalClaims,
            requestTimestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint that requires Admin role.
    /// Demonstrates role-based authorization.
    /// </summary>
    /// <returns>Admin-only data</returns>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdminOnlyData()
    {
        _logger.LogInformation("Admin endpoint accessed by User {UserId} ({UserRole}) from Company {CompanyId}",
            _tenantContext.UserId, _tenantContext.UserRole, _tenantContext.CompanyId);

        return Ok(new
        {
            message = "This data is only available to Admin users",
            accessedBy = new
            {
                userId = _tenantContext.UserId,
                userName = _tenantContext.UserName,
                userRole = _tenantContext.UserRole,
                companyId = _tenantContext.CompanyId,
                companyName = _tenantContext.CompanyName
            },
            requestTimestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint that requires Manager or Admin role.
    /// Demonstrates multiple role authorization.
    /// </summary>
    /// <returns>Manager-level data</returns>
    [HttpGet("manager-or-admin")]
    [Authorize(Roles = "Manager,Admin")]
    public IActionResult GetManagerData()
    {
        _logger.LogInformation("Manager endpoint accessed by User {UserId} ({UserRole}) from Company {CompanyId}",
            _tenantContext.UserId, _tenantContext.UserRole, _tenantContext.CompanyId);

        return Ok(new
        {
            message = "This data is available to Manager and Admin users",
            accessedBy = new
            {
                userId = _tenantContext.UserId,
                userName = _tenantContext.UserName,
                userRole = _tenantContext.UserRole,
                companyId = _tenantContext.CompanyId,
                companyName = _tenantContext.CompanyName
            },
            requestTimestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Public endpoint that doesn't require authentication.
    /// Shows what happens when no tenant context is available.
    /// </summary>
    /// <returns>Public data</returns>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublicData()
    {
        _logger.LogInformation("Public endpoint accessed. Authenticated: {IsAuthenticated}", 
            _tenantContext.IsAuthenticated);

        return Ok(new
        {
            message = "This is public data, no authentication required",
            isAuthenticated = _tenantContext.IsAuthenticated,
            companyId = _tenantContext.CompanyId, // Will be null if not authenticated
            requestTimestamp = DateTime.UtcNow
        });
    }
}