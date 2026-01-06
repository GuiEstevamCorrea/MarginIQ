# External System Integration (ERP/CRM)

## Overview

This module implements the integration layer for connecting MarginIQ with external ERP and CRM systems, as specified in **Projeto.md section 7.1**.

The implementation follows **hexagonal architecture** principles:
- **Application Layer**: Defines ports (interfaces) and use cases
- **Infrastructure Layer** (future): Will implement adapters for specific systems
- **Asynchronous & Decoupled**: All integrations are designed to be non-blocking

## Supported Systems (Future)

1. **SAP ERP**
2. **TOTVS Protheus/RM**
3. **CSV/Excel Files**
4. **Salesforce CRM**
5. **Microsoft Dynamics**
6. **Oracle ERP**
7. **Generic REST API**
8. **Custom integrations**

## Architecture

```
Application
├── Ports
│   └── IExternalSystemIntegrationService.cs    // Port interface
│
└── UseCases
    └── ImportCustomersFromExternalSystemUseCase.cs    // Import use case
```

Future Infrastructure adapters:
```
Infrastructure
└── ExternalIntegrations
    ├── SAPAdapter.cs           // SAP specific implementation
    ├── TOTVSAdapter.cs         // TOTVS specific implementation
    ├── CSVAdapter.cs           // File import implementation
    └── GenericAPIAdapter.cs    // Generic REST API implementation
```

## Key Features

### 1. Multi-Tenant Isolation
All integrations respect company boundaries. Data from Company A never mixes with Company B.

### 2. Import/Export Capabilities
- **Import**: Bring customer, product, and user data from external systems
- **Export**: Send approved discount requests back to ERP/CRM for deal closure
- **Sync**: Bidirectional synchronization to keep data in sync

### 3. Import Modes
- **Full Import**: Replaces all data (useful for initial setup)
- **Incremental Import**: Only imports new/changed records (efficient for ongoing sync)

### 4. Dry Run Mode
Test imports without persisting data - validate CSV files before actual import.

### 5. Comprehensive Logging
All integration operations are logged for:
- Troubleshooting
- Audit compliance
- Performance monitoring

### 6. Error Handling
- Detailed error messages for each failed record
- Warnings for non-critical issues
- Transaction-like behavior (all or nothing for critical operations)

### 7. External ID Tracking
Entities store `ExternalSystemId` for bidirectional mapping:
```csharp
customer.SetExternalId("SAP-CUST-12345");
```
This enables sync operations and prevents duplicates.

## Port Interface: IExternalSystemIntegrationService

### Methods

#### IsIntegrationAvailableAsync
Checks if a specific integration is configured and healthy for a company.

#### ImportCustomersAsync
Imports customer data from external system.
- Returns detailed statistics (imported, updated, skipped, failed)
- Supports dry run mode
- Logs all operations

#### ImportProductsAsync
Imports product data from external system.

#### ImportUsersAsync
Imports user/salesperson data from external system.

#### ExportApprovedDiscountsAsync
Exports approved discount requests to external system for deal closure.

#### SyncDataAsync
Performs bidirectional synchronization:
- Import from external → MarginIQ
- Export from MarginIQ → external
- Both directions

#### GetIntegrationStatusAsync
Returns health status and configuration details.

#### GetIntegrationLogsAsync
Retrieves integration logs for troubleshooting.

## Use Case: ImportCustomersFromExternalSystemUseCase

### Flow

1. **Validate Company**: Ensure company exists and is active
2. **Validate User**: Only Admins can import data (security)
3. **Call Integration Service**: Delegate to infrastructure adapter
4. **Map External Data**: Convert from external format to domain entities
5. **Create or Update**: 
   - Check if customer exists by `ExternalSystemId` or name
   - Create new or update existing
6. **Audit Log**: Record the import operation
7. **Return Results**: Detailed statistics and any errors

### Request DTO
```csharp
public class ImportCustomersRequest
{
    public Guid CompanyId { get; set; }
    public ExternalSystemType SystemType { get; set; }  // SAP, TOTVS, CSV, etc.
    public ImportMode Mode { get; set; }                // Full or Incremental
    public string? FilePath { get; set; }               // For CSV imports
    public string? FileContentBase64 { get; set; }      // Alternative for API uploads
    public string? ConnectionString { get; set; }       // For ERP connections
    public Guid InitiatedBy { get; set; }               // Admin user ID
    public DateTime? ModifiedSinceDate { get; set; }    // For incremental sync
    public bool DryRun { get; set; }                    // Test without persisting
}
```

### Response DTO
```csharp
public class ImportCustomersResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }       // Newly created
    public int UpdatedRecords { get; set; }        // Existing customers updated
    public int SkippedRecords { get; set; }        // Skipped (duplicates, validation)
    public int FailedRecords { get; set; }         // Failed with errors
    public List<string> Errors { get; set; }       // Detailed error messages
    public List<string> Warnings { get; set; }     // Non-critical issues
    public TimeSpan Duration { get; set; }         // Performance metric
    public List<ImportedCustomerSummary> ImportedCustomers { get; set; }
}
```

## Domain Changes

### Customer Entity
Added `ExternalSystemId` property for integration:
```csharp
public string? ExternalSystemId { get; private set; }

public void SetExternalId(string? externalSystemId)
{
    ExternalSystemId = externalSystemId;
    UpdatedAt = DateTime.UtcNow;
}
```

### ICustomerRepository
Added method to find by external ID:
```csharp
Task<Customer?> GetByExternalIdAsync(
    Guid companyId, 
    string externalSystemId, 
    CancellationToken cancellationToken = default);
```

### AuditAction Enum
Added actions for integration operations:
```csharp
DataImport = 12,
DataExport = 13,
DataSync = 14
```

## Example Usage

### CSV Import
```csharp
var request = new ImportCustomersRequest
{
    CompanyId = companyId,
    SystemType = ExternalSystemType.CSV,
    Mode = ImportMode.Full,
    FilePath = "c:/imports/customers.csv",
    InitiatedBy = adminUserId,
    DryRun = false
};

var response = await useCase.ExecuteAsync(request, cancellationToken);

if (response.Success)
{
    Console.WriteLine($"Imported {response.ImportedRecords} customers");
    Console.WriteLine($"Updated {response.UpdatedRecords} customers");
}
```

### SAP Integration (Future)
```csharp
var request = new ImportCustomersRequest
{
    CompanyId = companyId,
    SystemType = ExternalSystemType.SAP,
    Mode = ImportMode.Incremental,
    ConnectionString = "encrypted-sap-connection-string",
    ModifiedSinceDate = DateTime.UtcNow.AddDays(-7),  // Last week
    InitiatedBy = adminUserId
};
```

### Dry Run Test
```csharp
var request = new ImportCustomersRequest
{
    CompanyId = companyId,
    SystemType = ExternalSystemType.CSV,
    FilePath = "customers.csv",
    InitiatedBy = adminUserId,
    DryRun = true  // Just validate, don't persist
};

var response = await useCase.ExecuteAsync(request, cancellationToken);
// Check response.Errors and response.Warnings without affecting database
```

## Data Mapping

### CSV Format Example
```csv
ExternalId,Name,Segment,Classification,Email,Phone,IsActive
SAP-001,Acme Corp,Manufacturing,A,contact@acme.com,555-1234,true
SAP-002,TechStart,Technology,B,info@techstart.com,555-5678,true
SAP-003,Retail Co,Retail,C,sales@retailco.com,555-9012,false
```

### Classification Mapping
- `A` → CustomerClassification.A (top tier)
- `B` → CustomerClassification.B (mid tier)
- `C` → CustomerClassification.C (lower tier)
- Empty or invalid → CustomerClassification.Unclassified

## Future Implementation

### Infrastructure Adapters

#### CSVAdapter (Priority 1)
```csharp
public class CSVIntegrationAdapter : IExternalSystemIntegrationService
{
    public async Task<ImportResult<CustomerImportData>> ImportCustomersAsync(
        ImportRequest request, 
        CancellationToken cancellationToken)
    {
        // Read CSV file
        // Parse rows
        // Map to CustomerImportData
        // Return results
    }
}
```

#### SAPAdapter (Priority 2)
```csharp
public class SAPIntegrationAdapter : IExternalSystemIntegrationService
{
    private readonly ISAPClient _sapClient;
    
    public async Task<ImportResult<CustomerImportData>> ImportCustomersAsync(
        ImportRequest request, 
        CancellationToken cancellationToken)
    {
        // Connect to SAP RFC or OData
        // Query customer master data
        // Map to CustomerImportData
        // Return results
    }
}
```

#### TOTVSAdapter (Priority 3)
Similar pattern for TOTVS Protheus/RM integration.

### API Endpoints (Future)

```http
POST /api/integrations/import/customers
Content-Type: application/json

{
  "companyId": "guid",
  "systemType": "CSV",
  "mode": "Incremental",
  "fileContentBase64": "base64-encoded-csv",
  "initiatedBy": "admin-user-id",
  "dryRun": false
}

Response:
{
  "success": true,
  "message": "Successfully imported 150 customers",
  "totalRecords": 150,
  "importedRecords": 120,
  "updatedRecords": 30,
  "skippedRecords": 0,
  "failedRecords": 0,
  "errors": [],
  "warnings": ["Customer 'XYZ' has no email"],
  "duration": "00:00:02.5"
}
```

## Security Considerations

### Authentication & Authorization
- Only **Admin** role can import data
- Multi-tenant isolation enforced at all levels
- Connection strings should be encrypted in configuration

### Audit Trail
All import operations are logged:
- Who initiated the import
- What was imported
- When it happened
- Success/failure status
- Number of records affected

### Data Validation
- Name length validation (2-200 characters)
- Classification validation (A/B/C or Unclassified)
- Status validation (Active/Inactive)
- Company membership validation

## Performance Considerations

### Batch Processing
Import large datasets in batches:
```csharp
// Process in chunks of 1000 records
const int batchSize = 1000;
for (int i = 0; i < totalRecords; i += batchSize)
{
    var batch = records.Skip(i).Take(batchSize);
    await ProcessBatchAsync(batch);
}
```

### Async/Non-Blocking
All operations are async to avoid blocking the API:
```csharp
// Background job for large imports
var jobId = await _backgroundJobService.EnqueueAsync(
    () => _integrationService.ImportCustomersAsync(request));
```

### Caching
Cache external ID lookups to avoid N+1 queries:
```csharp
var customersByExternalId = (await _customerRepository.GetByCompanyIdAsync(companyId))
    .Where(c => c.ExternalSystemId != null)
    .ToDictionary(c => c.ExternalSystemId!);
```

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task ImportCustomers_WithValidCSV_ShouldCreateCustomers()
{
    // Arrange: Mock repositories and integration service
    // Act: Execute use case
    // Assert: Verify customers created, audit log written
}
```

### Integration Tests
```csharp
[Fact]
public async Task CSVAdapter_WithRealFile_ShouldParseCorrectly()
{
    // Test actual CSV parsing with sample file
}
```

### End-to-End Tests
```csharp
[Fact]
public async Task ImportCustomers_EndToEnd_ShouldSyncWithDatabase()
{
    // Upload CSV via API
    // Verify customers in database
    // Verify audit logs
}
```

## Monitoring & Observability

### Metrics to Track
- Import success rate
- Import duration (P50, P95, P99)
- Records per second throughput
- Error rates by system type
- Integration health status

### Logging
```csharp
_logger.LogInformation(
    "Starting customer import: Company={CompanyId}, System={SystemType}, Records={Count}",
    companyId, systemType, totalRecords);

_logger.LogError(
    "Customer import failed: Company={CompanyId}, Error={Error}",
    companyId, errorMessage);
```

## Roadmap

### Phase 1: Foundation (Current)
- ✅ Port interface defined
- ✅ Import customers use case
- ✅ Domain model updated
- ✅ Audit logging

### Phase 2: CSV Implementation
- ⏳ CSV adapter
- ⏳ File validation
- ⏳ API endpoint
- ⏳ UI for file upload

### Phase 3: ERP Integration
- ⏳ SAP adapter
- ⏳ TOTVS adapter
- ⏳ Connection management
- ⏳ Scheduled sync jobs

### Phase 4: Advanced Features
- ⏳ Product import use case
- ⏳ User import use case
- ⏳ Export approved discounts
- ⏳ Bidirectional sync
- ⏳ Conflict resolution
- ⏳ Webhook support

### Phase 5: Enterprise
- ⏳ Oracle adapter
- ⏳ Dynamics adapter
- ⏳ Salesforce adapter
- ⏳ Real-time sync
- ⏳ Advanced monitoring

## Conclusion

This integration layer provides a **solid foundation** for connecting MarginIQ with external systems. The hexagonal architecture ensures:

1. **Testability**: Use cases can be tested without real ERP connections
2. **Flexibility**: Easy to add new adapters for different systems
3. **Maintainability**: Clear separation of concerns
4. **Scalability**: Async design supports high-volume imports

The implementation aligns with **Projeto.md section 7.1** requirements:
- ✅ SAP support planned
- ✅ TOTVS support planned
- ✅ CSV/Excel support planned
- ✅ Asynchronous architecture
- ✅ Decoupled design
- ✅ Multi-tenant isolation

Next step: Implement CSV adapter in Infrastructure layer to enable the **first working integration** for customer onboarding.
