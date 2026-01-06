# Scalability Architecture - MarginIQ

## Overview

This document details the **Scalability implementation** for MarginIQ, following the requirements from **Projeto.md section 8.3**:
- ✅ Multi-tenant architecture
- ✅ AI isolated per company
- ✅ Growing historical data management

MarginIQ is designed to scale from **10 companies to 10,000 companies** without architectural changes.

---

## Architecture Principles

### Scalability Goals

| Metric | Target | Strategy |
|--------|--------|----------|
| **Companies** | 10,000+ tenants | Logical multi-tenancy |
| **Users** | 100,000+ users | Stateless API design |
| **Discount Requests** | 1M+ requests/month | Database indexing + partitioning |
| **API Response Time** | < 200ms (P95) | Caching + query optimization |
| **AI Training** | Per-company isolation | Logical model separation |
| **Historical Data** | 5+ years retention | Archiving + partitioning |
| **Concurrent Users** | 1,000+ simultaneous | Horizontal scaling |

### Key Design Decisions

1. **Logical Multi-Tenancy** (single database, CompanyId filter)
   - ✅ Lower cost than physical isolation
   - ✅ Simpler operations
   - ✅ Easier backup/recovery
   - ⚠️ Requires strict data isolation

2. **Stateless API Design**
   - ✅ Horizontal scaling without sticky sessions
   - ✅ Deploy without downtime
   - ✅ Load balancer friendly

3. **Async Processing**
   - ✅ Long operations don't block API
   - ✅ Better resource utilization
   - ✅ Retry on failures

4. **Distributed Caching**
   - ✅ Reduce database load
   - ✅ Faster responses
   - ✅ Share cache across API instances

---

## 1. Multi-Tenant Architecture

### 1.1 Logical Multi-Tenancy

All data is **logically isolated** by `CompanyId`:

```
┌─────────────────────────────────────────┐
│         Single Database                 │
│  ┌───────────────────────────────────┐  │
│  │  Table: DiscountRequests          │  │
│  │  ├─ Id (PK)                       │  │
│  │  ├─ CompanyId (FK) ← ISOLATION   │  │
│  │  ├─ CustomerId                    │  │
│  │  ├─ ...                           │  │
│  │  └─ Index: CompanyId, CreatedAt  │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │  Table: Users                     │  │
│  │  ├─ Id (PK)                       │  │
│  │  ├─ CompanyId (FK) ← ISOLATION   │  │
│  │  ├─ Email                         │  │
│  │  └─ Index: CompanyId, Email      │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Every query MUST filter by `CompanyId`**:

```csharp
// ✅ CORRECT
var requests = await _context.DiscountRequests
    .Where(r => r.CompanyId == companyId)
    .Where(r => r.Status == DiscountRequestStatus.UnderReview)
    .ToListAsync();

// ❌ DANGEROUS - Exposes all companies' data
var requests = await _context.DiscountRequests
    .Where(r => r.Status == DiscountRequestStatus.UnderReview)
    .ToListAsync();
```

### 1.2 Database Isolation Enforcement

#### Global Query Filter (EF Core)

```csharp
public class MarginIQDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global filter to all entities with CompanyId
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ICompanyScoped).IsAssignableFrom(entityType.ClrType))
            {
                var method = SetGlobalQueryMethod.MakeGenericMethod(entityType.ClrType);
                method.Invoke(this, new object[] { modelBuilder });
            }
        }
    }
    
    private static readonly MethodInfo SetGlobalQueryMethod = 
        typeof(MarginIQDbContext).GetMethod(nameof(SetGlobalQuery), 
            BindingFlags.NonPublic | BindingFlags.Static);
    
    private static void SetGlobalQuery<T>(ModelBuilder builder) where T : class, ICompanyScoped
    {
        builder.Entity<T>().HasQueryFilter(e => 
            e.CompanyId == GetCurrentCompanyId());
    }
    
    private static Guid GetCurrentCompanyId()
    {
        // Get from HttpContext (JWT claims)
        // This is evaluated at query execution time
        return /* current company ID */;
    }
}
```

#### Interface for Company-Scoped Entities

```csharp
public interface ICompanyScoped
{
    Guid CompanyId { get; }
}

public abstract class BaseEntity : ICompanyScoped
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
```

### 1.3 Multi-Tenant Testing

Verify isolation with automated tests:

```csharp
[Fact]
public async Task GetDiscountRequests_ShouldOnlyReturnOwnCompanyData()
{
    // Arrange
    var company1 = await CreateCompanyAsync("Company 1");
    var company2 = await CreateCompanyAsync("Company 2");
    
    await CreateDiscountRequestAsync(company1.Id, count: 5);
    await CreateDiscountRequestAsync(company2.Id, count: 3);
    
    // Act - Query as Company 1
    var requests = await _repo.GetAllAsync(company1.Id);
    
    // Assert - Should only see Company 1's requests
    Assert.Equal(5, requests.Count);
    Assert.All(requests, r => Assert.Equal(company1.Id, r.CompanyId));
}

[Fact]
public async Task CreateDiscountRequest_ShouldNotAccessOtherCompanyData()
{
    // Arrange
    var company1 = await CreateCompanyAsync("Company 1");
    var company2 = await CreateCompanyAsync("Company 2");
    
    var customer1 = await CreateCustomerAsync(company1.Id);
    var customer2 = await CreateCustomerAsync(company2.Id);
    
    // Act & Assert - Company 1 tries to use Company 2's customer
    await Assert.ThrowsAsync<NotFoundException>(async () =>
    {
        await _useCase.ExecuteAsync(new CreateDiscountRequestRequest
        {
            CompanyId = company1.Id,
            CustomerId = customer2.Id  // Wrong company!
        });
    });
}
```

### 1.4 Multi-Tenant Monitoring

Track tenant metrics:

```csharp
public class TenantMetrics
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; }
    
    // Usage metrics
    public int ActiveUsers { get; set; }
    public int TotalUsers { get; set; }
    public int DiscountRequestsThisMonth { get; set; }
    public int APIRequestsThisMonth { get; set; }
    
    // Performance metrics
    public TimeSpan AverageResponseTime { get; set; }
    public long DatabaseRowCount { get; set; }
    public long StorageUsageBytes { get; set; }
    
    // AI metrics
    public int AITrainingRecords { get; set; }
    public decimal AIAutoApprovalRate { get; set; }
    public DateTime? LastAITraining { get; set; }
}
```

---

## 2. AI Isolation Per Company

### 2.1 Logical AI Model Isolation

Each company has its own **logical AI model**:

```
┌─────────────────────────────────────────┐
│         AI Service Architecture         │
│                                         │
│  ┌───────────────────────────────────┐  │
│  │  Request: Recommend Discount      │  │
│  │  CompanyId: company-1-guid        │  │
│  └───────────────────────────────────┘  │
│                ↓                        │
│  ┌───────────────────────────────────┐  │
│  │  AI Service                       │  │
│  │  • Filter training data           │  │
│  │  • WHERE CompanyId = company-1    │  │
│  │  • Train/predict with isolated data│ │
│  └───────────────────────────────────┘  │
│                ↓                        │
│  ┌───────────────────────────────────┐  │
│  │  AILearningData Table             │  │
│  │  ├─ Company 1 (1000 records)      │  │
│  │  ├─ Company 2 (5000 records)      │  │
│  │  └─ Company 3 (500 records)       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Key principle**: AI never sees data from other companies.

### 2.2 AI Training Data Isolation

```csharp
public class AIService : IAIService
{
    public async Task<TrainingResult> TrainModelAsync(
        ModelTrainingRequest request, 
        CancellationToken cancellationToken)
    {
        // 1. Get training data ONLY for this company
        var trainingData = await _learningDataRepo.GetAllAsync(
            companyId: request.CompanyId,
            since: request.TrainingSince);
        
        // 2. Verify all records belong to company
        if (trainingData.Any(d => d.CompanyId != request.CompanyId))
        {
            throw new InvalidOperationException(
                "Training data contamination detected");
        }
        
        // 3. Train model with isolated data
        var model = await _mlEngine.TrainAsync(trainingData);
        
        // 4. Save model with company tag
        await _modelRepo.SaveAsync(new AIModel
        {
            CompanyId = request.CompanyId,
            ModelData = model.Serialize(),
            TrainedAt = DateTime.UtcNow,
            RecordCount = trainingData.Count
        });
        
        return new TrainingResult
        {
            Success = true,
            DataPointsProcessed = trainingData.Count,
            TrainedAt = DateTime.UtcNow
        };
    }
}
```

### 2.3 AI Prediction Isolation

```csharp
public async Task<DiscountRecommendation> RecommendDiscountAsync(
    DiscountRecommendationRequest request,
    CancellationToken cancellationToken)
{
    // 1. Load model for this company ONLY
    var model = await _modelRepo.GetLatestAsync(request.CompanyId);
    
    if (model == null)
    {
        // No model trained yet - use fallback
        return await FallbackRecommendationAsync(request);
    }
    
    // 2. Get company-specific features
    var features = await BuildFeaturesAsync(request, request.CompanyId);
    
    // 3. Predict using company's model
    var prediction = await _mlEngine.PredictAsync(model, features);
    
    return new DiscountRecommendation
    {
        RecommendedDiscountPercentage = prediction.Discount,
        ExpectedMarginPercentage = prediction.Margin,
        Confidence = prediction.Confidence,
        Explanation = "Based on your company's historical data"
    };
}
```

### 2.4 AI Model Storage Strategy

#### Option 1: Database Storage (Small Models)

```csharp
public class AIModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }  // ISOLATION
    public byte[] ModelData { get; set; }  // Serialized model
    public DateTime TrainedAt { get; set; }
    public int RecordCount { get; set; }
    public string ModelVersion { get; set; }
}
```

**Pros**:
- Simple backup/restore
- Easy querying
- Transactional consistency

**Cons**:
- Database size grows
- Large models slow down queries

**Best for**: Models < 50 MB

#### Option 2: Blob Storage (Large Models)

```csharp
public class AIModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BlobPath { get; set; }  // "models/company-1/v5.bin"
    public DateTime TrainedAt { get; set; }
    public long ModelSizeBytes { get; set; }
}

// Load model from blob storage
public async Task<MLModel> LoadModelAsync(Guid companyId)
{
    var modelMetadata = await _context.AIModels
        .Where(m => m.CompanyId == companyId)
        .OrderByDescending(m => m.TrainedAt)
        .FirstAsync();
    
    var modelBytes = await _blobStorage.DownloadAsync(modelMetadata.BlobPath);
    return MLModel.Deserialize(modelBytes);
}
```

**Pros**:
- Unlimited model size
- Faster database queries
- Cheaper storage

**Cons**:
- External dependency
- More complex backup

**Best for**: Models > 50 MB

### 2.5 AI Cold Start Problem

When a new company joins, they have **no training data**:

```csharp
public class AIGovernanceService
{
    public async Task<AIGovernanceSettings> GetSettingsAsync(Guid companyId)
    {
        var settings = await _repo.GetByCompanyIdAsync(companyId);
        
        // Check if company has enough training data
        var trainingRecordCount = await _learningDataRepo.CountAsync(companyId);
        
        if (trainingRecordCount < 100)
        {
            // Not enough data - disable AI
            settings.AIEnabled = false;
            settings.Reason = $"Insufficient training data ({trainingRecordCount}/100 required)";
        }
        
        return settings;
    }
}
```

**Cold start strategy**:
1. Company starts with **AI disabled**
2. System collects data (100+ discount requests)
3. Admin manually enables AI when ready
4. First training uses all historical data
5. Incremental training thereafter

---

## 3. Historical Data Management

### 3.1 Data Growth Projections

**Typical company** (100 users, 1000 requests/month):

| Year | Discount Requests | Audit Logs | Database Size |
|------|-------------------|------------|---------------|
| 1 | 12,000 | 50,000 | 500 MB |
| 2 | 24,000 | 100,000 | 1 GB |
| 3 | 36,000 | 150,000 | 1.5 GB |
| 5 | 60,000 | 250,000 | 2.5 GB |

**System-wide** (1000 companies):

| Year | Total Requests | Total Audit Logs | Database Size |
|------|----------------|------------------|---------------|
| 1 | 12M | 50M | 500 GB |
| 2 | 24M | 100M | 1 TB |
| 3 | 36M | 150M | 1.5 TB |
| 5 | 60M | 250M | 2.5 TB |

### 3.2 Partitioning Strategy

#### Table Partitioning by Date

Partition large tables by year/month:

```sql
-- Partition DiscountRequests by CreatedAt
CREATE TABLE DiscountRequests_2025 (
    CHECK (CreatedAt >= '2025-01-01' AND CreatedAt < '2026-01-01')
) INHERITS (DiscountRequests);

CREATE TABLE DiscountRequests_2026 (
    CHECK (CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01')
) INHERITS (DiscountRequests);

-- Indexes on each partition
CREATE INDEX idx_discountrequests_2025_companyid 
    ON DiscountRequests_2025(CompanyId, CreatedAt);

CREATE INDEX idx_discountrequests_2026_companyid 
    ON DiscountRequests_2026(CompanyId, CreatedAt);
```

**Benefits**:
- Faster queries (scan only relevant partition)
- Easier archiving (drop old partitions)
- Better vacuum performance

#### Partition by CompanyId (Sharding)

For very large deployments (10,000+ companies):

```sql
-- Shard by CompanyId hash
CREATE TABLE DiscountRequests_Shard0 (
    CHECK (hashtext(CompanyId::text) % 10 = 0)
) INHERITS (DiscountRequests);

CREATE TABLE DiscountRequests_Shard1 (
    CHECK (hashtext(CompanyId::text) % 10 = 1)
) INHERITS (DiscountRequests);

-- ... Shard2 through Shard9
```

**Benefits**:
- Distribute load across multiple disks
- Parallel query execution
- Horizontal database scaling

### 3.3 Archiving Strategy

Move old data to archive storage:

```csharp
public class ArchiveHistoricalDataJob
{
    public async Task ExecuteAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-5);  // Archive data > 5 years
        
        // 1. Export to archive storage (S3, Azure Blob)
        var oldRequests = await _context.DiscountRequests
            .Where(r => r.CreatedAt < cutoffDate)
            .Take(10000)  // Batch size
            .ToListAsync();
        
        if (oldRequests.Any())
        {
            // 2. Save to archive
            var archiveFile = $"archive/discount-requests/{cutoffDate:yyyy-MM}.parquet";
            await _archiveStorage.SaveAsync(archiveFile, oldRequests);
            
            // 3. Delete from primary database
            _context.DiscountRequests.RemoveRange(oldRequests);
            await _context.SaveChangesAsync();
            
            // 4. Log archiving
            _logger.LogInformation(
                "Archived {Count} discount requests older than {Date}",
                oldRequests.Count, cutoffDate);
        }
    }
}
```

**Archive format**:
- **Parquet**: Columnar, compressed, queryable
- **CSV/JSON**: Simple but larger
- **Database snapshot**: Restore to separate DB

**Archive access**:
- Read-only access via separate API
- Slower queries (acceptable for old data)
- On-demand restore for legal/audit needs

### 3.4 Data Retention Policy

| Data Type | Hot Storage | Archive | Total Retention |
|-----------|-------------|---------|-----------------|
| Discount Requests | 1 year | 4 years | 5 years |
| Approvals | 1 year | 6 years | 7 years (compliance) |
| Audit Logs | 1 year | 6 years | 7 years (GDPR/LGPD) |
| AI Training Data | 2 years | 3 years | 5 years |
| User Data | Active + 30 days | - | Until deletion request |

### 3.5 Database Indexing

**Critical indexes for scalability**:

```sql
-- DiscountRequests (most queried table)
CREATE INDEX idx_discountrequests_company_status_created 
    ON DiscountRequests(CompanyId, Status, CreatedAt DESC);

CREATE INDEX idx_discountrequests_company_salesperson 
    ON DiscountRequests(CompanyId, SalespersonId, CreatedAt DESC);

CREATE INDEX idx_discountrequests_company_customer 
    ON DiscountRequests(CompanyId, CustomerId, CreatedAt DESC);

-- Users
CREATE INDEX idx_users_company_email 
    ON Users(CompanyId, Email);

CREATE INDEX idx_users_company_role_status 
    ON Users(CompanyId, Role, Status);

-- Customers
CREATE INDEX idx_customers_company_name 
    ON Customers(CompanyId, Name);

-- AuditLogs
CREATE INDEX idx_auditlogs_company_timestamp 
    ON AuditLogs(CompanyId, Timestamp DESC);

CREATE INDEX idx_auditlogs_company_entity 
    ON AuditLogs(CompanyId, EntityName, EntityId);

-- AILearningData
CREATE INDEX idx_ailearningdata_company_timestamp 
    ON AILearningData(CompanyId, Timestamp DESC);
```

**Index monitoring**:

```sql
-- Find missing indexes
SELECT 
    schemaname,
    tablename,
    attname,
    n_distinct,
    correlation
FROM pg_stats
WHERE schemaname = 'public'
    AND n_distinct > 100
    AND correlation < 0.1
ORDER BY n_distinct DESC;

-- Find unused indexes
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan
FROM pg_stat_user_indexes
WHERE idx_scan = 0
    AND indexrelname NOT LIKE '%_pkey';
```

---

## 4. Caching Strategy

### 4.1 Cache Layers

```
┌─────────────────────────────────────────┐
│          API Layer                      │
│  ┌───────────────────────────────────┐  │
│  │  In-Memory Cache (L1)             │  │  ← Fast, per-instance
│  │  • Static data (10 min TTL)       │  │
│  │  • User sessions                  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│      Distributed Cache (L2)             │
│  ┌───────────────────────────────────┐  │
│  │  Redis                            │  │  ← Shared across instances
│  │  • AI responses (5 min TTL)       │  │
│  │  • Query results (2 min TTL)      │  │
│  │  • Company settings (15 min TTL)  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│         Database (Source of Truth)      │
└─────────────────────────────────────────┘
```

### 4.2 Cache Key Strategy

**Pattern**: `{entity}:{companyId}:{identifier}:{version}`

```csharp
public class CacheKeyBuilder
{
    public static string ForUser(Guid userId, Guid companyId) =>
        $"user:{companyId}:{userId}:v1";
    
    public static string ForCompanySettings(Guid companyId) =>
        $"company:settings:{companyId}:v1";
    
    public static string ForDiscountRequest(Guid requestId, Guid companyId) =>
        $"discount:request:{companyId}:{requestId}:v1";
    
    public static string ForAIRecommendation(DiscountRecommendationRequest request) =>
    {
        var hash = ComputeHash(request);
        return $"ai:recommendation:{request.CompanyId}:{hash}:v1";
    }
}
```

### 4.3 Cache Invalidation

**Write-through pattern**:

```csharp
public class DiscountRequestService
{
    private readonly IDistributedCache _cache;
    
    public async Task UpdateAsync(DiscountRequest request)
    {
        // 1. Update database
        await _repo.UpdateAsync(request);
        
        // 2. Invalidate cache
        var cacheKey = CacheKeyBuilder.ForDiscountRequest(request.Id, request.CompanyId);
        await _cache.RemoveAsync(cacheKey);
        
        // 3. Invalidate related caches
        var listCacheKey = $"discount:list:{request.CompanyId}:*";
        await _cache.RemoveByPatternAsync(listCacheKey);
    }
}
```

**Time-based expiration**:

```csharp
// Cache with TTL
await _cache.SetAsync(cacheKey, value, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
});
```

### 4.4 Cache Monitoring

Track cache effectiveness:

```csharp
public class CacheMetrics
{
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
    public double HitRate => (double)CacheHits / TotalRequests * 100;
    
    public TimeSpan AverageHitLatency { get; set; }  // Should be < 10ms
    public TimeSpan AverageMissLatency { get; set; }  // Database query time
    
    public long MemoryUsageBytes { get; set; }
    public int KeyCount { get; set; }
    public int EvictionCount { get; set; }
}
```

**Target metrics**:
- Cache hit rate: > 70%
- Cache latency: < 10ms
- Memory usage: < 1GB per API instance

---

## 5. Background Jobs

### 5.1 Job Types

| Job | Frequency | Purpose | Duration |
|-----|-----------|---------|----------|
| AI Training | Daily (off-peak) | Train models with new data | 5-30 min |
| Data Archiving | Monthly | Move old data to archive | 1-2 hours |
| Cache Warmup | On deployment | Pre-populate cache | 5 min |
| Metrics Aggregation | Hourly | Calculate KPIs | 2-5 min |
| Notification Queue | Every minute | Send pending notifications | < 1 min |
| Token Cleanup | Daily | Delete expired refresh tokens | 1 min |

### 5.2 Job Implementation (Hangfire)

```csharp
// In Program.cs
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;  // Concurrent jobs
});

// Schedule recurring jobs
RecurringJob.AddOrUpdate<AITrainingJob>(
    "ai-training",
    job => job.ExecuteAsync(),
    Cron.Daily(2));  // 2 AM daily

RecurringJob.AddOrUpdate<ArchiveHistoricalDataJob>(
    "data-archiving",
    job => job.ExecuteAsync(),
    Cron.Monthly(1, 3));  // 3 AM on 1st of month
```

### 5.3 Job Isolation (Multi-Tenant)

Each job processes **one company at a time**:

```csharp
public class AITrainingJob
{
    public async Task ExecuteAsync()
    {
        // Get all companies with AI enabled
        var companies = await _companyRepo.GetAllWithAIEnabledAsync();
        
        foreach (var company in companies)
        {
            try
            {
                // Process one company
                await TrainCompanyModelAsync(company.Id);
                
                _logger.LogInformation(
                    "AI training completed for company {CompanyId}",
                    company.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AI training failed for company {CompanyId}",
                    company.Id);
                
                // Continue with next company (don't fail entire job)
            }
        }
    }
    
    private async Task TrainCompanyModelAsync(Guid companyId)
    {
        // Train model for single company
        var trainingData = await _learningDataRepo.GetAllAsync(companyId);
        
        if (trainingData.Count < 100)
        {
            _logger.LogWarning(
                "Insufficient training data for company {CompanyId}: {Count} records",
                companyId, trainingData.Count);
            return;
        }
        
        await _aiService.TrainModelAsync(new ModelTrainingRequest
        {
            CompanyId = companyId,
            TrainingSince = DateTime.UtcNow.AddMonths(-6)
        });
    }
}
```

### 5.4 Job Monitoring

```csharp
// Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

**Monitor**:
- Job success/failure rate
- Job duration trends
- Failed job retry count
- Queue length

---

## 6. Horizontal Scaling

### 6.1 Stateless API Design

**Requirements for horizontal scaling**:

✅ **No in-memory session state**
- Use JWT tokens (client-side)
- Or distributed cache (Redis) for sessions

✅ **No file system storage**
- Use blob storage (S3, Azure Blob)
- Or database for uploads

✅ **No background threads in API**
- Use Hangfire/Quartz for background jobs
- Separate worker processes

✅ **Database connection pooling**
- Share connections across requests
- Configure max pool size

### 6.2 Load Balancer Configuration

```
┌─────────────────────────────────────────┐
│         Load Balancer (Azure/AWS)       │
│  • Round-robin                          │
│  • Health checks                        │
│  • Sticky sessions: OFF                 │
└─────────────────────────────────────────┘
          ↓           ↓           ↓
    ┌─────────┐ ┌─────────┐ ┌─────────┐
    │  API 1  │ │  API 2  │ │  API 3  │
    │ 2 vCPU  │ │ 2 vCPU  │ │ 2 vCPU  │
    │ 4 GB    │ │ 4 GB    │ │ 4 GB    │
    └─────────┘ └─────────┘ └─────────┘
          ↓           ↓           ↓
    ┌─────────────────────────────────┐
    │      Shared Redis Cache         │
    └─────────────────────────────────┘
          ↓           ↓           ↓
    ┌─────────────────────────────────┐
    │      Database (Primary)         │
    │      + Read Replicas (Optional) │
    └─────────────────────────────────┘
```

### 6.3 Health Checks

```csharp
// In Program.cs
services.AddHealthChecks()
    .AddDbContextCheck<MarginIQDbContext>("database")
    .AddRedis(redisConnectionString, "redis")
    .AddHangfire(hangfireConfig, "hangfire")
    .AddCheck<AIServiceHealthCheck>("ai-service");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Health check endpoint**:
```json
GET /health

{
  "status": "Healthy",
  "totalDuration": "00:00:00.123",
  "entries": {
    "database": { "status": "Healthy", "duration": "00:00:00.045" },
    "redis": { "status": "Healthy", "duration": "00:00:00.012" },
    "hangfire": { "status": "Healthy", "duration": "00:00:00.008" },
    "ai-service": { "status": "Healthy", "duration": "00:00:00.058" }
  }
}
```

### 6.4 Auto-Scaling Rules

**Scale out** (add instances) when:
- CPU > 70% for 5 minutes
- Memory > 80% for 5 minutes
- Request queue > 100 for 2 minutes

**Scale in** (remove instances) when:
- CPU < 30% for 10 minutes
- Memory < 40% for 10 minutes
- Request queue = 0 for 10 minutes

**Minimum instances**: 2 (high availability)
**Maximum instances**: 10 (cost control)

---

## 7. Database Scaling

### 7.1 Read Replicas

For read-heavy workloads:

```
┌─────────────────────────────────────────┐
│         API Layer                       │
└─────────────────────────────────────────┘
          ↓                        ↓
    [Writes only]           [Reads only]
          ↓                        ↓
┌─────────────────┐    ┌─────────────────┐
│ Primary DB      │───→│ Read Replica 1  │
│ (Master)        │    │                 │
└─────────────────┘    └─────────────────┘
                             ↓
                    ┌─────────────────┐
                    │ Read Replica 2  │
                    │                 │
                    └─────────────────┘
```

**Configuration**:

```csharp
services.AddDbContext<MarginIQDbContext>(options =>
{
    // Primary connection (writes)
    options.UseSqlServer(primaryConnectionString);
});

services.AddDbContext<MarginIQReadOnlyDbContext>(options =>
{
    // Read replica connection (reads)
    options.UseSqlServer(replicaConnectionString);
});
```

**Usage**:

```csharp
public class GetDiscountRequestHistoryUseCase
{
    private readonly MarginIQReadOnlyDbContext _readContext;  // Read replica
    
    public async Task<List<DiscountRequest>> ExecuteAsync(Guid companyId)
    {
        // Use read replica for queries
        return await _readContext.DiscountRequests
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync();
    }
}
```

### 7.2 Connection Pooling

```csharp
// In connection string
"Server=localhost;Database=MarginIQ;User Id=sa;Password=***;
 Min Pool Size=10;
 Max Pool Size=100;
 Connection Timeout=30;
 Command Timeout=60;"
```

**Best practices**:
- Min pool size: 10 (keep connections warm)
- Max pool size: 100 (prevent connection exhaustion)
- Connection timeout: 30s (fail fast)
- Command timeout: 60s (long-running queries)

### 7.3 Query Optimization

**Slow query detection**:

```csharp
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    public override async Task<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken)
    {
        if (eventData.Duration.TotalMilliseconds > 1000)  // > 1 second
        {
            _logger.LogWarning(
                "Slow query detected: {Query} ({Duration}ms)",
                command.CommandText,
                eventData.Duration.TotalMilliseconds);
        }
        
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
```

**Common optimizations**:
- Add missing indexes (see section 3.5)
- Use pagination (`Take/Skip`)
- Avoid `N+1` queries (use `Include`)
- Use `AsNoTracking()` for read-only queries
- Use compiled queries for hot paths

---

## 8. Monitoring & Observability

### 8.1 Key Metrics

**System metrics**:
- CPU usage (per instance)
- Memory usage (per instance)
- Disk I/O (database)
- Network throughput

**Application metrics**:
- Request rate (requests/second)
- Response time (P50, P95, P99)
- Error rate (%)
- Cache hit rate (%)

**Business metrics**:
- Active companies
- Active users
- Discount requests (per day)
- AI auto-approval rate
- Average approval time

### 8.2 Distributed Tracing

```csharp
// OpenTelemetry configuration
services.AddOpenTelemetryTracing(builder =>
{
    builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddRedisInstrumentation()
        .AddJaegerExporter();
});
```

**Trace example**:
```
Request: POST /api/discount-requests
├─ [50ms] Validate JWT token
├─ [120ms] Get user from database
├─ [200ms] Calculate margin
├─ [1500ms] Call AI service
│  ├─ [100ms] Load AI model
│  ├─ [1200ms] Predict discount
│  └─ [200ms] Cache result
└─ [80ms] Save to database

Total: 1950ms
```

### 8.3 Alerting Rules

| Alert | Condition | Severity | Action |
|-------|-----------|----------|--------|
| High error rate | > 5% errors for 5 min | Critical | Page on-call |
| Slow responses | P95 > 2s for 10 min | Warning | Investigate |
| Database down | Health check fails | Critical | Page on-call |
| Redis down | Health check fails | Warning | Switch to in-memory |
| Disk space low | < 20% free | Warning | Scale storage |
| High CPU | > 80% for 15 min | Warning | Scale out |
| High memory | > 90% for 5 min | Critical | Scale out |

---

## 9. Cost Optimization

### 9.1 Resource Right-Sizing

**Development**:
- 1 API instance (1 vCPU, 2 GB)
- 1 Database (Basic tier)
- No cache

**Production** (100 companies):
- 2 API instances (2 vCPU, 4 GB each)
- Database (Standard tier, 100 DTU)
- Redis (1 GB)

**Production** (1000 companies):
- 5 API instances (4 vCPU, 8 GB each)
- Database (Premium tier, 500 DTU) + 2 read replicas
- Redis (10 GB)

### 9.2 Data Retention Cost

**Strategy**:
- Hot storage (database): 1 year = 100 GB = $100/month
- Archive storage (S3/Blob): 4 years = 400 GB = $10/month
- **Total**: $110/month vs $500/month (no archiving)

**Savings**: 78% reduction in storage cost

### 9.3 Cache Cost vs Database Cost

**Without cache**:
- Database: 1000 DTU @ $500/month
- Total: $500/month

**With cache**:
- Database: 500 DTU @ $250/month (50% reduction)
- Redis: 10 GB @ $50/month
- Total: $300/month

**Savings**: 40% reduction

---

## 10. Deployment Strategy

### 10.1 Blue-Green Deployment

```
┌─────────────────────────────────────────┐
│         Load Balancer                   │
└─────────────────────────────────────────┘
          ↓
    [Route 100% traffic to Blue]
          ↓
┌─────────────────┐    ┌─────────────────┐
│ Blue (Current)  │    │ Green (New)     │
│ Version 1.5     │    │ Version 1.6     │
│ • 3 instances   │    │ • 3 instances   │
│ • Production DB │    │ • Production DB │
└─────────────────┘    └─────────────────┘

[Deploy + Test Green]
[Switch traffic: Blue → Green]
[Keep Blue for 24h rollback window]
[Terminate Blue]
```

### 10.2 Database Migrations

**Zero-downtime migration strategy**:

```csharp
// Step 1: Add new column (nullable)
migrationBuilder.AddColumn<string>(
    name: "NewColumn",
    table: "DiscountRequests",
    nullable: true);

// Step 2: Deploy code that writes to both old and new columns
// (Backward compatible)

// Step 3: Backfill data
UPDATE DiscountRequests SET NewColumn = OldColumn WHERE NewColumn IS NULL;

// Step 4: Make column NOT NULL
migrationBuilder.AlterColumn<string>(
    name: "NewColumn",
    table: "DiscountRequests",
    nullable: false);

// Step 5: Drop old column (next release)
migrationBuilder.DropColumn(
    name: "OldColumn",
    table: "DiscountRequests");
```

---

## Conclusion

MarginIQ is designed for **scale from day one**:

1. ✅ **Multi-Tenant Architecture** - Logical isolation with CompanyId filter
2. ✅ **AI Isolation** - Each company's model trained on their data only
3. ✅ **Historical Data Management** - Partitioning + archiving for 5+ years
4. ✅ **Distributed Caching** - Redis for 70%+ cache hit rate
5. ✅ **Background Jobs** - Hangfire for async processing
6. ✅ **Horizontal Scaling** - Stateless API design, auto-scaling
7. ✅ **Database Scaling** - Read replicas, connection pooling, indexing
8. ✅ **Monitoring** - OpenTelemetry, health checks, alerting
9. ✅ **Cost Optimization** - Right-sizing, archiving, cache strategy
10. ✅ **Zero-Downtime Deployment** - Blue-green, gradual rollout

**Capacity**: Ready to scale from **10 to 10,000 companies** without architectural changes.
