using System.Security.Claims;

using Infrastructure.Data;

namespace Api.Middleware;

/// <summary>
/// Middleware that extracts tenant (company) information from JWT token claims.
/// Sets up tenant context for multi-tenant isolation and validation.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request and extracts tenant information from JWT claims.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="tenantContext">Tenant context service</param>
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        try
        {
            // Skip tenant validation for authentication endpoints and health checks
            if (ShouldSkipTenantValidation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // If user is authenticated, validate tenant context
            if (context.User.Identity?.IsAuthenticated == true)
            {
                await ValidateTenantContext(context, tenantContext);
            }

            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt from {IpAddress} to {Path}", 
                context.Connection.RemoteIpAddress, context.Request.Path);
            
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Invalid tenant context");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant middleware for {Path}", context.Request.Path);
            
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    /// <summary>
    /// Determines if tenant validation should be skipped for the given path.
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if tenant validation should be skipped</returns>
    private static bool ShouldSkipTenantValidation(PathString path)
    {
        var pathsToSkip = new[]
        {
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/logout",
            "/health",
            "/swagger",
            "/favicon.ico"
        };

        return pathsToSkip.Any(skipPath => 
            path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates that the authenticated user has valid tenant context.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="tenantContext">Tenant context</param>
    private async Task ValidateTenantContext(HttpContext context, ITenantContext tenantContext)
    {
        // Extract tenant information from claims
        var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Validate required claims are present
        if (string.IsNullOrEmpty(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
        {
            _logger.LogWarning("Missing or invalid CompanyId claim for user {UserId}", userIdClaim);
            throw new UnauthorizedAccessException("Invalid tenant context: Missing CompanyId");
        }

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Missing or invalid UserId claim");
            throw new UnauthorizedAccessException("Invalid tenant context: Missing UserId");
        }

        // Log tenant context for audit purposes
        _logger.LogDebug("Tenant context established - CompanyId: {CompanyId}, UserId: {UserId}, Role: {Role}",
            companyId, userId, context.User.FindFirst(ClaimTypes.Role)?.Value);

        // Add tenant information to request items for later use
        context.Items["TenantId"] = companyId;
        context.Items["UserId"] = userId;
        context.Items["UserRole"] = context.User.FindFirst(ClaimTypes.Role)?.Value;
        context.Items["CompanyName"] = context.User.FindFirst("CompanyName")?.Value;

        // Propagate tenant id into DbContext for global query filters
        var db = context.RequestServices.GetService<MarginIQDbContext>();
        if (db != null)
        {
            db.CurrentCompanyId = companyId;
        }

        // Validate tenant status (could be extended to check if company is active)
        await ValidateTenantStatus(companyId, userId);
    }

    /// <summary>
    /// Validates that the tenant (company) is active and user has access.
    /// This could be extended to check database for company status.
    /// </summary>
    /// <param name="companyId">Company ID</param>
    /// <param name="userId">User ID</param>
    private static Task ValidateTenantStatus(Guid companyId, Guid userId)
    {
        // For now, just validate that IDs are not empty
        // In a full implementation, you might check:
        // - Company is active/not suspended
        // - User has not been disabled
        // - Company subscription is valid
        // - User belongs to the company

        if (companyId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid tenant: Empty CompanyId");
        }

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid tenant: Empty UserId");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for registering tenant middleware.
/// </summary>
public static class TenantMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant context services to the service collection.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        return services;
    }

    /// <summary>
    /// Uses tenant middleware in the application pipeline.
    /// Should be called after UseAuthentication() and before UseAuthorization().
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}