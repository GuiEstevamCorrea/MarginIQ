namespace Api.Middleware;

/// <summary>
/// Context for storing tenant-specific information during request processing.
/// Provides multi-tenant isolation by extracting CompanyId from JWT token.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant (company) ID from the JWT token
    /// </summary>
    Guid? CompanyId { get; }

    /// <summary>
    /// Gets the current tenant (company) name from the JWT token
    /// </summary>
    string? CompanyName { get; }

    /// <summary>
    /// Gets the current user ID from the JWT token
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user name from the JWT token
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the current user role from the JWT token
    /// </summary>
    string? UserRole { get; }

    /// <summary>
    /// Gets the current user email from the JWT token
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Gets whether the current request has valid tenant context
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets additional claims from the JWT token
    /// </summary>
    IDictionary<string, string> AdditionalClaims { get; }
}

/// <summary>
/// Implementation of tenant context that extracts information from HTTP context claims.
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CompanyId
    {
        get
        {
            var companyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyId")?.Value;
            return Guid.TryParse(companyIdClaim, out var companyId) ? companyId : null;
        }
    }

    public string? CompanyName => _httpContextAccessor.HttpContext?.User?.FindFirst("CompanyName")?.Value;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

    public string? UserRole => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

    public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true && CompanyId.HasValue;

    public IDictionary<string, string> AdditionalClaims
    {
        get
        {
            var claims = new Dictionary<string, string>();
            var user = _httpContextAccessor.HttpContext?.User;
            
            if (user != null)
            {
                foreach (var claim in user.Claims)
                {
                    if (!claims.ContainsKey(claim.Type))
                    {
                        claims[claim.Type] = claim.Value;
                    }
                }
            }
            
            return claims;
        }
    }
}