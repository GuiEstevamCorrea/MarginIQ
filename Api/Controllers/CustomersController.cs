using Api.Middleware;
using Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Real business controller for managing customers with multi-tenant isolation.
/// Demonstrates how tenant context is used in production controllers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerRepository customerRepository,
        ITenantContext tenantContext,
        ILogger<CustomersController> logger)
    {
        _customerRepository = customerRepository;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets all customers for the current company (tenant).
    /// Automatically filtered by CompanyId from JWT token.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                return BadRequest("Invalid tenant context");
            }

            _logger.LogInformation("Fetching customers for company {CompanyId} by user {UserId}", 
                _tenantContext.CompanyId, _tenantContext.UserId);

            // Use the correct repository method
            var customers = await _customerRepository.GetByCompanyIdAsync(_tenantContext.CompanyId.Value, cancellationToken);

            return Ok(new
            {
                tenantInfo = new
                {
                    companyId = _tenantContext.CompanyId,
                    companyName = _tenantContext.CompanyName,
                    requestedBy = _tenantContext.UserName
                },
                customers = customers.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    segment = c.Segment,
                    classification = c.Classification.ToString(),
                    status = c.Status.ToString(),
                    createdAt = c.CreatedAt
                }).ToList(),
                totalCount = customers.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers for company {CompanyId}", _tenantContext.CompanyId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a specific customer by ID, ensuring multi-tenant isolation.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.CompanyId.HasValue)
            {
                return BadRequest("Invalid tenant context");
            }

            _logger.LogInformation("Fetching customer {CustomerId} for company {CompanyId} by user {UserId}", 
                id, _tenantContext.CompanyId, _tenantContext.UserId);

            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);

            // Ensure customer belongs to the current company (tenant isolation)
            if (customer == null || customer.CompanyId != _tenantContext.CompanyId.Value)
            {
                return NotFound($"Customer {id} not found or not accessible");
            }

            return Ok(new
            {
                tenantInfo = new
                {
                    companyId = _tenantContext.CompanyId,
                    companyName = _tenantContext.CompanyName
                },
                customer = new
                {
                    id = customer.Id,
                    name = customer.Name,
                    segment = customer.Segment,
                    classification = customer.Classification.ToString(),
                    status = customer.Status.ToString(),
                    externalSystemId = customer.ExternalSystemId,
                    additionalInfo = customer.AdditionalInfo,
                    createdAt = customer.CreatedAt,
                    updatedAt = customer.UpdatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId} for company {CompanyId}", id, _tenantContext.CompanyId);
            return StatusCode(500, "Internal server error");
        }
    }
}