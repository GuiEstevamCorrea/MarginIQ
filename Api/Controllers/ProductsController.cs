using Api.Middleware;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Real business controller for managing products with multi-tenant isolation.
/// Shows proper use of tenant context in production code.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository productRepository,
        ITenantContext tenantContext,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all products for the current company.
    /// Demonstrates tenant isolation in business controllers.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                return BadRequest("Invalid tenant context");
            }

            _logger.LogInformation("Fetching products for company {CompanyId} by user {UserId}, category: {Category}", 
                _tenantContext.CompanyId, _tenantContext.UserId, category ?? "all");

            var products = await _productRepository.GetByCompanyIdAsync(_tenantContext.CompanyId.Value, cancellationToken);

            // Apply category filter if specified
            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => 
                    string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(new
            {
                tenantInfo = new
                {
                    companyId = _tenantContext.CompanyId,
                    companyName = _tenantContext.CompanyName,
                    requestedBy = _tenantContext.UserName
                },
                products = products.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    category = p.Category,
                    sku = p.Sku,
                    basePrice = new
                    {
                        amount = p.BasePrice.Value,
                        currency = p.BasePrice.Currency
                    },
                    baseMarginPercentage = p.BaseMarginPercentage,
                    status = p.Status.ToString()
                }).ToList(),
                totalCount = products.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products for company {CompanyId}", _tenantContext.CompanyId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific product by ID with tenant isolation.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                return BadRequest("Invalid tenant context");
            }

            var product = await _productRepository.GetByIdAsync(id, cancellationToken);

            // Ensure product belongs to current company
            if (product == null || product.CompanyId != _tenantContext.CompanyId.Value)
            {
                return NotFound($"Product {id} not found or not accessible");
            }

            return Ok(new
            {
                tenantInfo = new
                {
                    companyId = _tenantContext.CompanyId,
                    companyName = _tenantContext.CompanyName
                },
                product = new
                {
                    id = product.Id,
                    name = product.Name,
                    category = product.Category,
                    sku = product.Sku,
                    basePrice = new
                    {
                        amount = product.BasePrice.Value,
                        currency = product.BasePrice.Currency
                    },
                    baseMarginPercentage = product.BaseMarginPercentage,
                    status = product.Status.ToString(),
                    additionalInfo = product.AdditionalInfo,
                    createdAt = product.CreatedAt,
                    updatedAt = product.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} for company {CompanyId}", id, _tenantContext.CompanyId);
            return StatusCode(500, "Internal server error");
        }
    }
}