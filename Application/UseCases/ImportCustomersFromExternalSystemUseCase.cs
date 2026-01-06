using Application.Ports;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.UseCases;

/// <summary>
/// Use case for importing customers from external systems (ERP/CRM).
/// 
/// This is a future integration capability that allows companies to:
/// - Import customer data from SAP, TOTVS, or CSV files
/// - Keep customer data synchronized
/// - Avoid manual data entry
/// 
/// Flow:
/// 1. Validate company and user permissions
/// 2. Call external integration service
/// 3. Map external data to domain entities
/// 4. Create or update customers
/// 5. Log all operations for audit
/// 6. Return detailed results
/// </summary>
public class ImportCustomersFromExternalSystemUseCase
{
    private readonly IExternalSystemIntegrationService _integrationService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ImportCustomersFromExternalSystemUseCase(
        IExternalSystemIntegrationService integrationService,
        ICustomerRepository customerRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository)
    {
        _integrationService = integrationService;
        _customerRepository = customerRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ImportCustomersResponse> ExecuteAsync(
        ImportCustomersRequest request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate company exists and is active
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company == null)
        {
            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = "Company not found"
            };
        }

        if (company.Status != CompanyStatus.Active)
        {
            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = "Company is not active"
            };
        }

        // Step 2: Validate user has Admin role (only Admins can import)
        var user = await _userRepository.GetByIdAsync(request.InitiatedBy, cancellationToken);
        if (user == null)
        {
            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = "User not found"
            };
        }

        if (user.Role != UserRole.Admin)
        {
            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = "Only administrators can import customers from external systems"
            };
        }

        if (user.CompanyId != request.CompanyId)
        {
            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = "User does not belong to the specified company"
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Step 3: Call external integration service to import data
            var integrationRequest = new ImportRequest
            {
                CompanyId = request.CompanyId,
                SystemType = request.SystemType,
                Mode = request.Mode,
                FilePath = request.FilePath,
                FileContentBase64 = request.FileContentBase64,
                ConnectionString = request.ConnectionString,
                ConfigurationJson = request.ConfigurationJson,
                InitiatedBy = request.InitiatedBy,
                ModifiedSinceDate = request.ModifiedSinceDate,
                DryRun = request.DryRun
            };

            var importResult = await _integrationService.ImportCustomersAsync(
                integrationRequest,
                cancellationToken);

            if (!importResult.Success)
            {
                // Log failed import
                await LogImportOperationAsync(
                    request.CompanyId,
                    request.InitiatedBy,
                    request.SystemType,
                    false,
                    importResult.TotalRecords,
                    0,
                    0,
                    string.Join(", ", importResult.Errors),
                    cancellationToken);

                return new ImportCustomersResponse
                {
                    Success = false,
                    ErrorMessage = "Import failed: " + string.Join(", ", importResult.Errors),
                    TotalRecords = importResult.TotalRecords,
                    ImportedRecords = 0,
                    UpdatedRecords = 0,
                    SkippedRecords = importResult.SkippedRecords,
                    FailedRecords = importResult.FailedRecords,
                    Errors = importResult.Errors,
                    Warnings = importResult.Warnings,
                    Duration = DateTime.UtcNow - startTime
                };
            }

            // Step 4: If dry run, just return results without persisting
            if (request.DryRun)
            {
                return new ImportCustomersResponse
                {
                    Success = true,
                    Message = "Dry run completed successfully. No data was persisted.",
                    TotalRecords = importResult.TotalRecords,
                    ImportedRecords = 0,
                    UpdatedRecords = 0,
                    SkippedRecords = importResult.SkippedRecords,
                    FailedRecords = importResult.FailedRecords,
                    Errors = importResult.Errors,
                    Warnings = importResult.Warnings,
                    Duration = DateTime.UtcNow - startTime,
                    ImportedCustomers = importResult.ImportedData
                        .Select(d => new ImportedCustomerSummary
                        {
                            ExternalId = d.ExternalId,
                            Name = d.Name,
                            Segment = d.Segment,
                            Classification = d.Classification,
                            WouldBeCreated = true
                        })
                        .ToList()
                };
            }

            // Step 5: Map and persist customer data
            int createdCount = 0;
            int updatedCount = 0;
            var createdCustomers = new List<ImportedCustomerSummary>();

            foreach (var customerData in importResult.ImportedData)
            {
                try
                {
                    // Check if customer already exists by external ID or name
                    var existingCustomer = await _customerRepository.GetByExternalIdAsync(
                        request.CompanyId,
                        customerData.ExternalId,
                        cancellationToken);

                    if (existingCustomer == null)
                    {
                        // Try to find by name (case insensitive)
                        var customersByName = await _customerRepository.GetByCompanyIdAsync(
                            request.CompanyId,
                            cancellationToken);
                        existingCustomer = customersByName
                            .FirstOrDefault(c => c.Name.Equals(customerData.Name, StringComparison.OrdinalIgnoreCase));
                    }

                    if (existingCustomer == null)
                    {
                        // Create new customer
                        var classification = ParseClassification(customerData.Classification);
                        
                        var newCustomer = new Customer(
                            customerData.Name,
                            request.CompanyId,
                            customerData.Segment,
                            classification);

                        // Set external ID for future sync
                        newCustomer.SetExternalId(customerData.ExternalId);

                        if (!customerData.IsActive)
                        {
                            newCustomer.Deactivate();
                        }

                        await _customerRepository.AddAsync(newCustomer);
                        createdCount++;

                        createdCustomers.Add(new ImportedCustomerSummary
                        {
                            ExternalId = customerData.ExternalId,
                            Name = customerData.Name,
                            Segment = customerData.Segment,
                            Classification = customerData.Classification,
                            WouldBeCreated = false,
                            WasCreated = true,
                            CustomerId = newCustomer.Id
                        });
                    }
                    else
                    {
                        // Update existing customer
                        var classification = ParseClassification(customerData.Classification);
                        
                        existingCustomer.UpdateName(customerData.Name);
                        existingCustomer.UpdateSegment(customerData.Segment);
                        existingCustomer.UpdateClassification(classification);
                        existingCustomer.SetExternalId(customerData.ExternalId);

                        if (customerData.IsActive && existingCustomer.Status != CustomerStatus.Active)
                        {
                            existingCustomer.Activate();
                        }
                        else if (!customerData.IsActive && existingCustomer.Status == CustomerStatus.Active)
                        {
                            existingCustomer.Deactivate();
                        }

                        await _customerRepository.UpdateAsync(existingCustomer);
                        updatedCount++;

                        createdCustomers.Add(new ImportedCustomerSummary
                        {
                            ExternalId = customerData.ExternalId,
                            Name = customerData.Name,
                            Segment = customerData.Segment,
                            Classification = customerData.Classification,
                            WouldBeCreated = false,
                            WasCreated = false,
                            WasUpdated = true,
                            CustomerId = existingCustomer.Id
                        });
                    }
                }
                catch (Exception ex)
                {
                    importResult.Errors.Add($"Failed to process customer '{customerData.Name}': {ex.Message}");
                }
            }

            // Step 6: Log successful import
            await LogImportOperationAsync(
                request.CompanyId,
                request.InitiatedBy,
                request.SystemType,
                true,
                importResult.TotalRecords,
                createdCount,
                updatedCount,
                null,
                cancellationToken);

            return new ImportCustomersResponse
            {
                Success = true,
                Message = $"Successfully imported {createdCount} new customers and updated {updatedCount} existing customers",
                TotalRecords = importResult.TotalRecords,
                ImportedRecords = createdCount,
                UpdatedRecords = updatedCount,
                SkippedRecords = importResult.SkippedRecords,
                FailedRecords = importResult.FailedRecords,
                Errors = importResult.Errors,
                Warnings = importResult.Warnings,
                Duration = DateTime.UtcNow - startTime,
                ImportedCustomers = createdCustomers
            };
        }
        catch (Exception ex)
        {
            // Log failed import
            await LogImportOperationAsync(
                request.CompanyId,
                request.InitiatedBy,
                request.SystemType,
                false,
                0,
                0,
                0,
                ex.Message,
                cancellationToken);

            return new ImportCustomersResponse
            {
                Success = false,
                ErrorMessage = $"Import failed with exception: {ex.Message}",
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task LogImportOperationAsync(
        Guid companyId,
        Guid userId,
        ExternalSystemType systemType,
        bool success,
        int totalRecords,
        int createdRecords,
        int updatedRecords,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                SystemType = systemType.ToString(),
                Success = success,
                TotalRecords = totalRecords,
                CreatedRecords = createdRecords,
                UpdatedRecords = updatedRecords,
                ErrorMessage = errorMessage
            });

            var auditLog = AuditLog.CreateForHuman(
                "Customer",
                companyId, // Using companyId as entityId for company-level operation
                AuditAction.DataImport,
                companyId,
                userId,
                payload);

            await _auditLogRepository.AddAsync(auditLog);
        }
        catch
        {
            // Audit log failure should not break the import
            // Could be logged to a separate system here
        }
    }

    /// <summary>
    /// Parses classification string to enum
    /// </summary>
    private static CustomerClassification ParseClassification(string? classification)
    {
        if (string.IsNullOrWhiteSpace(classification))
            return CustomerClassification.Unclassified;

        return classification.ToUpperInvariant() switch
        {
            "A" => CustomerClassification.A,
            "B" => CustomerClassification.B,
            "C" => CustomerClassification.C,
            _ => CustomerClassification.Unclassified
        };
    }
}

/// <summary>
/// Request to import customers from external system
/// </summary>
public class ImportCustomersRequest
{
    public Guid CompanyId { get; set; }
    public ExternalSystemType SystemType { get; set; }
    public ImportMode Mode { get; set; } = ImportMode.Incremental;
    public string? FilePath { get; set; }
    public string? FileContentBase64 { get; set; }
    public string? ConnectionString { get; set; }
    public string? ConfigurationJson { get; set; }
    public Guid InitiatedBy { get; set; }
    public DateTime? ModifiedSinceDate { get; set; }
    public bool DryRun { get; set; }
}

/// <summary>
/// Response for customer import operation
/// </summary>
public class ImportCustomersResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public List<ImportedCustomerSummary> ImportedCustomers { get; set; } = new();
}

/// <summary>
/// Summary of an imported customer
/// </summary>
public class ImportedCustomerSummary
{
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Segment { get; set; }
    public string? Classification { get; set; }
    public bool WouldBeCreated { get; set; }
    public bool WasCreated { get; set; }
    public bool WasUpdated { get; set; }
    public Guid? CustomerId { get; set; }
}
