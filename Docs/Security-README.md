# Security Architecture - MarginIQ

## Overview

This document details the **Security implementation** for MarginIQ, following the requirements from **Projeto.md section 8.2**:
- ✅ JWT authentication
- ✅ Role-based authorization
- ✅ Comprehensive audit logging
- ✅ Multi-tenant data isolation
- ✅ GDPR/LGPD compliance

Security is built into every layer following **hexagonal architecture** principles.

---

## Architecture

### Security Layers

```
┌─────────────────────────────────────────┐
│           API Layer (HTTP)              │
│  ┌───────────────────────────────────┐  │
│  │  Authentication Middleware        │  │  ← JWT Validation
│  │  Authorization Filter             │  │  ← Role Check
│  │  Multi-Tenant Filter              │  │  ← Company Isolation
│  │  Audit Logging Middleware         │  │  ← Log Everything
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│        Application Layer (Use Cases)    │
│  ┌───────────────────────────────────┐  │
│  │  Authorization Checks             │  │  ← Domain Logic
│  │  Audit Log Recording              │  │  ← Business Events
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│       Infrastructure Layer (Adapters)   │
│  ┌───────────────────────────────────┐  │
│  │  IAuthenticationService           │  │  ← JWT Generation
│  │  IAuthorizationService            │  │  ← Permission Checks
│  │  IAuditLogRepository              │  │  ← Persistence
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## 1. Authentication (JWT)

### 1.1 JWT Token Structure

**Access Token** (short-lived, 15-60 minutes):
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id-guid",
    "email": "user@example.com",
    "name": "John Doe",
    "role": "Manager",
    "companyId": "company-id-guid",
    "companyName": "Acme Corp",
    "jti": "token-id-guid",
    "iat": 1735689600,
    "exp": 1735693200
  }
}
```

**Refresh Token** (long-lived, 7-30 days):
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id-guid",
    "jti": "refresh-token-id-guid",
    "iat": 1735689600,
    "exp": 1738281600
  }
}
```

### 1.2 Authentication Flow

#### Login Flow

```
1. User sends credentials
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "SecureP@ssw0rd"
   }

2. Server validates credentials
   - Check email exists
   - Verify password hash (bcrypt/Argon2)
   - Check user is active
   - Check company is active

3. Server generates tokens
   - Access token (15 min)
   - Refresh token (7 days)
   - Store refresh token in database

4. Server returns tokens
   {
     "success": true,
     "accessToken": "eyJhbGc...",
     "refreshToken": "eyJhbGc...",
     "accessTokenExpiresAt": "2026-01-06T15:00:00Z",
     "refreshTokenExpiresAt": "2026-01-13T14:00:00Z",
     "user": {
       "id": "user-id",
       "name": "John Doe",
       "email": "user@example.com",
       "role": "Manager",
       "companyId": "company-id",
       "companyName": "Acme Corp"
     }
   }

5. Client stores tokens
   - Access token: Memory or sessionStorage (never localStorage)
   - Refresh token: HttpOnly cookie (preferred) or secure storage
```

#### Token Refresh Flow

```
1. Access token expires (15 min)
   - Client detects 401 Unauthorized
   - OR proactively checks expiration

2. Client requests new access token
   POST /api/auth/refresh
   {
     "refreshToken": "eyJhbGc..."
   }

3. Server validates refresh token
   - Check token exists in database
   - Check not revoked
   - Check not expired
   - Check user still active

4. Server generates new access token
   - New access token (15 min)
   - Optionally rotate refresh token

5. Server returns new tokens
   {
     "success": true,
     "accessToken": "eyJhbGc...",
     "refreshToken": "eyJhbGc...",  // Optional: new refresh token
     "accessTokenExpiresAt": "2026-01-06T15:15:00Z"
   }
```

#### Logout Flow

```
1. User clicks logout
   POST /api/auth/logout
   Authorization: Bearer {accessToken}
   {
     "refreshToken": "eyJhbGc..."
   }

2. Server revokes refresh token
   - Mark refresh token as revoked in database
   - Access token remains valid until expiration (short-lived)

3. Client clears tokens
   - Remove from memory/storage
   - Remove cookies
```

### 1.3 Password Security

#### Password Requirements

- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number
- At least 1 special character
- Not in common password list (top 10,000)

#### Password Hashing

Use **bcrypt** (recommended) or **Argon2**:

```csharp
// Hash password on registration/change
string hashedPassword = _authService.HashPassword(plainPassword);

// Verify password on login
bool isValid = _authService.VerifyPassword(plainPassword, hashedPassword);
```

**Never**:
- Store passwords in plain text
- Log passwords
- Send passwords in emails
- Display passwords in UI
- Use weak hashing (MD5, SHA1)

#### Password Reset Flow

```
1. User requests password reset
   POST /api/auth/forgot-password
   {
     "email": "user@example.com"
   }

2. Server generates reset token
   - Create secure random token (256-bit)
   - Store token with 1-hour expiration
   - Send email with reset link

3. User clicks reset link
   GET /reset-password?token={token}&userId={userId}

4. User submits new password
   POST /api/auth/reset-password
   {
     "userId": "user-id",
     "resetToken": "secure-token",
     "newPassword": "NewSecureP@ssw0rd"
   }

5. Server validates and resets
   - Check token valid and not expired
   - Hash new password
   - Update user password
   - Revoke all refresh tokens (logout all devices)
   - Send confirmation email
```

### 1.4 Two-Factor Authentication (2FA)

Optional for MVP, recommended for production:

```
1. User enables 2FA
   POST /api/auth/enable-2fa
   
2. Server generates 2FA code
   - 6-digit TOTP code
   - Valid for 5 minutes
   - Send via email/SMS

3. User enters 2FA code on login
   POST /api/auth/login
   {
     "email": "user@example.com",
     "password": "SecureP@ssw0rd",
     "twoFactorCode": "123456"
   }

4. Server validates 2FA code
   - Check code valid and not expired
   - Check code not already used
   - Issue tokens on success
```

### 1.5 Security Best Practices

1. **Token Storage**
   - ✅ Access token: Memory or sessionStorage
   - ✅ Refresh token: HttpOnly cookie (CSRF protection required)
   - ❌ NEVER store tokens in localStorage (XSS vulnerability)

2. **Token Expiration**
   - Access token: 15-60 minutes (short)
   - Refresh token: 7-30 days (long)
   - Password reset token: 1 hour

3. **Token Revocation**
   - Store refresh tokens in database
   - Mark as revoked on logout
   - Revoke all tokens on password change
   - Revoke all tokens on security incident

4. **Rate Limiting**
   - Login: 5 attempts per 15 minutes per email
   - Password reset: 3 attempts per hour per email
   - Token refresh: 10 attempts per minute per user

5. **Brute Force Protection**
   - Lock account after 10 failed login attempts
   - Require password reset to unlock
   - Send security alert email

---

## 2. Authorization (Roles & Permissions)

### 2.1 Role Hierarchy

MarginIQ has **3 primary roles**:

| Role | Description | Typical Users |
|------|-------------|---------------|
| **Salesperson** | Creates discount requests | Sales reps, Account managers |
| **Manager** | Approves/rejects requests | Sales managers, Regional managers |
| **Admin** | Full system access | IT admins, System administrators |

### 2.2 Permission Matrix

Complete permission matrix for all roles:

| Permission | Salesperson | Manager | Admin |
|------------|-------------|---------|-------|
| **Discount Requests** |
| Create discount request | ✅ | ✅ | ✅ |
| View own requests | ✅ | ✅ | ✅ |
| View all requests | ❌ | ✅ | ✅ |
| Edit own draft requests | ✅ | ❌ | ❌ |
| Approve requests | ❌ | ✅ | ✅ |
| Reject requests | ❌ | ✅ | ✅ |
| Request adjustments | ❌ | ✅ | ✅ |
| **AI & Governance** |
| View AI decisions | ✅ | ✅ | ✅ |
| Override AI decisions | ❌ | ✅ | ✅ |
| Configure AI settings | ❌ | ❌ | ✅ |
| Enable/disable AI | ❌ | ❌ | ✅ |
| **Data Management** |
| View customers | ✅ | ✅ | ✅ |
| Manage customers | ❌ | ✅ | ✅ |
| View products | ✅ | ✅ | ✅ |
| Manage products | ❌ | ✅ | ✅ |
| Import data | ❌ | ❌ | ✅ |
| Export data | ❌ | ✅ | ✅ |
| **Business Rules** |
| View business rules | ✅ | ✅ | ✅ |
| Manage business rules | ❌ | ✅ | ✅ |
| **Audit & Security** |
| View audit logs | ❌ | ✅ | ✅ |
| View own audit history | ✅ | ✅ | ✅ |
| **User Management** |
| View users | ❌ | ✅ | ✅ |
| Manage users | ❌ | ❌ | ✅ |
| Assign roles | ❌ | ❌ | ✅ |
| **Reporting** |
| View basic reports | ✅ | ✅ | ✅ |
| View advanced analytics | ❌ | ✅ | ✅ |
| View company dashboard | ❌ | ✅ | ✅ |

### 2.3 Authorization Implementation

#### Role-Based Checks

```csharp
// In use case
public class ApproveDiscountRequestUseCase
{
    private readonly IAuthorizationService _authService;
    
    public async Task ExecuteAsync(Guid userId, Guid requestId)
    {
        // 1. Check user can approve discounts
        if (!await _authService.CanApproveDiscountsAsync(userId))
        {
            throw new UnauthorizedException("User does not have permission to approve discounts");
        }
        
        // 2. Check user belongs to request's company (multi-tenant)
        var request = await _requestRepo.GetByIdAsync(requestId);
        if (!await _authService.BelongsToCompanyAsync(userId, request.CompanyId))
        {
            throw new UnauthorizedException("User does not have access to this company's data");
        }
        
        // 3. Proceed with approval logic
        // ...
    }
}
```

#### Resource-Based Checks

```csharp
// Check if user can view specific request
public class GetDiscountRequestUseCase
{
    public async Task ExecuteAsync(Guid userId, Guid requestId)
    {
        // Check if user can view this specific request
        if (!await _authService.CanViewDiscountRequestAsync(userId, requestId))
        {
            throw new UnauthorizedException("User cannot view this discount request");
        }
        
        // Return request
        return await _requestRepo.GetByIdAsync(requestId);
    }
}
```

#### API Level Authorization

```csharp
// In controller
[HttpPost("discount-requests/{id}/approve")]
[Authorize(Roles = "Manager,Admin")]  // Declarative
public async Task<IActionResult> ApproveDiscountRequest(Guid id)
{
    // Role check already done by [Authorize] attribute
    var userId = User.GetUserId();  // From JWT claims
    
    await _approveUseCase.ExecuteAsync(userId, id);
    return Ok();
}
```

### 2.4 Authorization Best Practices

1. **Defense in Depth**
   - Authorization at API level (declarative)
   - Authorization at use case level (imperative)
   - Authorization at repository level (query filters)

2. **Fail Secure**
   - Default deny (no permissions by default)
   - Explicit grants only
   - Check permissions before every sensitive operation

3. **Least Privilege**
   - Grant minimum necessary permissions
   - Salespersons can only see their own requests
   - Managers can see company requests
   - Admins can see everything (but still company-scoped)

4. **Audit Everything**
   - Log all authorization checks
   - Log all permission changes
   - Log all role assignments

---

## 3. Multi-Tenant Data Isolation

### 3.1 Architecture

Every entity is **company-scoped**:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }  // CRITICAL
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
```

**Rule**: All database queries MUST filter by `CompanyId`.

### 3.2 Isolation Implementation

#### Repository Level

```csharp
public class DiscountRequestRepository : IDiscountRequestRepository
{
    public async Task<DiscountRequest?> GetByIdAsync(Guid id, Guid companyId)
    {
        // ALWAYS include companyId in query
        return await _context.DiscountRequests
            .Where(r => r.Id == id && r.CompanyId == companyId)  // CRITICAL
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<DiscountRequest>> GetAllAsync(Guid companyId)
    {
        // ALWAYS filter by companyId
        return await _context.DiscountRequests
            .Where(r => r.CompanyId == companyId)  // CRITICAL
            .ToListAsync();
    }
}
```

#### Use Case Level

```csharp
public class CreateDiscountRequestUseCase
{
    public async Task ExecuteAsync(Guid userId, CreateDiscountRequestRequest request)
    {
        // 1. Get user's company from authentication context
        var user = await _userRepo.GetByIdAsync(userId);
        var companyId = user.CompanyId;
        
        // 2. Create request with companyId
        var discountRequest = DiscountRequest.Create(
            companyId: companyId,  // CRITICAL
            customerId: request.CustomerId,
            salespersonId: userId,
            // ...
        );
        
        // 3. Verify customer belongs to same company
        var customer = await _customerRepo.GetByIdAsync(request.CustomerId, companyId);
        if (customer == null)
        {
            throw new NotFoundException("Customer not found in your company");
        }
        
        // 4. Save
        await _requestRepo.AddAsync(discountRequest);
    }
}
```

#### API Level

```csharp
public class DiscountRequestsController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetDiscountRequest(Guid id)
    {
        // 1. Get user's company from JWT claims
        var companyId = User.GetCompanyId();  // From JWT
        
        // 2. Pass companyId to use case (defense in depth)
        var request = await _getRequestUseCase.ExecuteAsync(id, companyId);
        
        return Ok(request);
    }
}
```

### 3.3 Multi-Tenant Security Checklist

✅ **Every query filters by `CompanyId`**
- [ ] All repository methods accept `companyId` parameter
- [ ] All LINQ queries include `.Where(x => x.CompanyId == companyId)`
- [ ] All SQL queries include `WHERE CompanyId = @companyId`

✅ **JWT contains `companyId` claim**
- [ ] CompanyId added to token on login
- [ ] CompanyId extracted from token on every request
- [ ] CompanyId validated on every use case

✅ **Cross-tenant access prevented**
- [ ] User cannot view other company's data
- [ ] User cannot create data for other company
- [ ] User cannot update other company's data
- [ ] User cannot delete other company's data

✅ **AI models isolated by company**
- [ ] AI training data filtered by CompanyId
- [ ] AI predictions scoped to CompanyId
- [ ] AI metrics calculated per CompanyId

✅ **Audit logs scoped by company**
- [ ] Audit logs include CompanyId
- [ ] Audit log queries filter by CompanyId
- [ ] Cross-tenant audit access prevented

### 3.4 Multi-Tenant Testing

```csharp
[Fact]
public async Task GetDiscountRequest_DifferentCompany_ShouldReturnNotFound()
{
    // Arrange
    var company1 = await CreateCompanyAsync("Company 1");
    var company2 = await CreateCompanyAsync("Company 2");
    
    var user1 = await CreateUserAsync(company1.Id, UserRole.Manager);
    var user2 = await CreateUserAsync(company2.Id, UserRole.Manager);
    
    var request1 = await CreateDiscountRequestAsync(company1.Id, user1.Id);
    
    // Act - User 2 tries to access Company 1's request
    var result = await _useCase.ExecuteAsync(request1.Id, company2.Id);
    
    // Assert
    Assert.Null(result);  // Should not find request from other company
}
```

---

## 4. Audit Logging

### 4.1 Audit Architecture

Every sensitive operation is logged to the `AuditLog` table:

```csharp
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityName { get; private set; }      // "DiscountRequest"
    public Guid EntityId { get; private set; }          // Discount request ID
    public AuditAction Action { get; private set; }     // Created, Approved, etc.
    public AuditOrigin Origin { get; private set; }     // Human or AI
    public Guid? UserId { get; private set; }           // Who did it (if human)
    public string? Payload { get; private set; }        // JSON of changes
    public Guid CompanyId { get; private set; }
    public DateTime Timestamp { get; private set; }
}
```

### 4.2 What to Audit

**Always audit**:
- ✅ All discount request operations (create, approve, reject, adjust)
- ✅ All AI decisions (recommendations, auto-approvals)
- ✅ All business rule changes
- ✅ All user operations (login, logout, password change)
- ✅ All role changes
- ✅ All AI governance changes
- ✅ All data imports/exports
- ✅ All configuration changes

**Payload should include**:
- Old value (for updates)
- New value
- User info
- IP address
- Timestamp
- Reason/justification

### 4.3 Audit Implementation

```csharp
public class ApproveDiscountRequestUseCase
{
    private readonly IAuditLogRepository _auditRepo;
    
    public async Task ExecuteAsync(Guid userId, Guid requestId, string justification)
    {
        // 1. Perform operation
        var request = await _requestRepo.GetByIdAsync(requestId);
        request.Approve(userId, justification);
        await _requestRepo.UpdateAsync(request);
        
        // 2. Log audit trail
        var auditLog = AuditLog.CreateForHuman(
            entityName: "DiscountRequest",
            entityId: requestId,
            action: AuditAction.Approved,
            companyId: request.CompanyId,
            userId: userId,
            payload: new
            {
                RequestId = requestId,
                RequestedDiscount = request.RequestedDiscountPercentage,
                ApprovedBy = userId,
                Justification = justification,
                Timestamp = DateTime.UtcNow
            }
        );
        
        await _auditRepo.AddAsync(auditLog);
    }
}
```

### 4.4 Audit Query Examples

```csharp
// Get all actions by user
public async Task<List<AuditLog>> GetUserActionsAsync(Guid userId, Guid companyId)
{
    return await _context.AuditLogs
        .Where(a => a.UserId == userId && a.CompanyId == companyId)
        .OrderByDescending(a => a.Timestamp)
        .ToListAsync();
}

// Get all AI decisions
public async Task<List<AuditLog>> GetAIDecisionsAsync(Guid companyId, DateTime since)
{
    return await _context.AuditLogs
        .Where(a => a.Origin == AuditOrigin.AI 
                 && a.CompanyId == companyId 
                 && a.Timestamp >= since)
        .OrderByDescending(a => a.Timestamp)
        .ToListAsync();
}

// Get history of specific entity
public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityName, Guid entityId)
{
    return await _context.AuditLogs
        .Where(a => a.EntityName == entityName && a.EntityId == entityId)
        .OrderBy(a => a.Timestamp)
        .ToListAsync();
}
```

### 4.5 Audit Retention

**Recommended retention policy**:
- Keep all audit logs for **7 years** (GDPR/LGPD requirement)
- Archive to cold storage after **1 year**
- Never delete audit logs (unless user exercises "right to be forgotten")

---

## 5. Data Protection (GDPR/LGPD)

### 5.1 Personal Data Inventory

MarginIQ stores the following personal data:

| Data Type | Location | Purpose | Retention |
|-----------|----------|---------|-----------|
| User name | `Users.Name` | Identification | Active + 30 days after deletion |
| User email | `Users.Email` | Authentication, notifications | Active + 30 days after deletion |
| Password hash | `Users.PasswordHash` | Authentication | Active + 30 days after deletion |
| Audit logs | `AuditLogs.*` | Compliance, security | 7 years |
| Customer data | `Customers.*` | Business operations | Active + 5 years |
| Discount requests | `DiscountRequests.*` | Business operations | Active + 5 years |

### 5.2 GDPR/LGPD Rights

#### Right to Access

Users can request all their personal data:

```csharp
public class GetPersonalDataUseCase
{
    public async Task<PersonalDataExport> ExecuteAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        var auditLogs = await _auditRepo.GetByUserIdAsync(userId);
        var requests = await _requestRepo.GetByUserIdAsync(userId);
        
        return new PersonalDataExport
        {
            User = user,
            AuditLogs = auditLogs,
            DiscountRequests = requests,
            ExportedAt = DateTime.UtcNow
        };
    }
}
```

#### Right to Deletion ("Right to be Forgotten")

User requests account deletion:

```csharp
public class DeleteUserDataUseCase
{
    public async Task ExecuteAsync(Guid userId, string reason)
    {
        // 1. Anonymize user data (don't delete - preserve audit trail)
        var user = await _userRepo.GetByIdAsync(userId);
        user.Anonymize();  // Set name to "Deleted User", email to "deleted@example.com"
        await _userRepo.UpdateAsync(user);
        
        // 2. Keep audit logs (legal requirement)
        // Do NOT delete audit logs
        
        // 3. Anonymize related data
        var requests = await _requestRepo.GetByUserIdAsync(userId);
        foreach (var request in requests)
        {
            // Keep request but anonymize creator
            request.AnonymizeCreator();
        }
        
        // 4. Log deletion
        await _auditRepo.AddAsync(AuditLog.CreateForHuman(
            entityName: "User",
            entityId: userId,
            action: AuditAction.Deleted,
            companyId: user.CompanyId,
            userId: userId,
            payload: new { Reason = reason }
        ));
    }
}
```

#### Right to Rectification

Users can update their personal data:

```csharp
public class UpdateUserProfileUseCase
{
    public async Task ExecuteAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        
        var oldData = new { user.Name, user.Email };
        
        user.UpdateProfile(request.Name, request.Email);
        await _userRepo.UpdateAsync(user);
        
        // Audit the change
        await _auditRepo.AddAsync(AuditLog.CreateForHuman(
            entityName: "User",
            entityId: userId,
            action: AuditAction.Updated,
            companyId: user.CompanyId,
            userId: userId,
            payload: new { OldData = oldData, NewData = request }
        ));
    }
}
```

#### Right to Portability

Export data in machine-readable format:

```csharp
public class ExportUserDataUseCase
{
    public async Task<byte[]> ExecuteAsync(Guid userId)
    {
        var data = await GetPersonalDataAsync(userId);
        
        // Export as JSON
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        return Encoding.UTF8.GetBytes(json);
    }
}
```

### 5.3 Data Encryption

**At Rest**:
- Database: Enable Transparent Data Encryption (TDE)
- Backups: Encrypted with AES-256
- File storage: Server-side encryption

**In Transit**:
- HTTPS only (TLS 1.2+)
- No HTTP allowed
- HSTS header enabled

**Application Level**:
```csharp
// Sensitive fields (optional for MarginIQ MVP)
public class User
{
    [Encrypted]  // Custom attribute for field-level encryption
    public string? TaxId { get; set; }  // CPF/CNPJ
}
```

### 5.4 Data Breach Response

**Plan**:
1. **Detect** - Monitor for unauthorized access
2. **Contain** - Revoke compromised tokens, lock accounts
3. **Assess** - Determine scope of breach
4. **Notify** - Inform users within 72 hours (GDPR requirement)
5. **Remediate** - Fix vulnerability, force password reset
6. **Report** - Report to authorities if required

---

## 6. API Security

### 6.1 Security Headers

```csharp
// In middleware
app.Use(async (context, next) =>
{
    // HTTPS enforcement
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
    
    // XSS protection
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    // CSP
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self'");
    
    await next();
});
```

### 6.2 CORS Configuration

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder
            .WithOrigins("https://app.marginiq.com")  // Production frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();  // For HttpOnly cookies
    });
});
```

### 6.3 Rate Limiting

```csharp
// Per user
[RateLimit(MaxRequests = 100, TimeWindow = "1m")]
public class DiscountRequestsController : ControllerBase
{
    // ...
}

// Per endpoint
[HttpPost("login")]
[RateLimit(MaxRequests = 5, TimeWindow = "15m")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // ...
}
```

### 6.4 Input Validation

```csharp
public class CreateDiscountRequestRequest
{
    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; }
    
    [Required]
    [Range(0, 100)]
    public decimal RequestedDiscountPercentage { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Justification { get; set; }
}
```

### 6.5 SQL Injection Prevention

**Always use parameterized queries**:

```csharp
// ✅ SAFE
var users = await _context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// ❌ DANGEROUS
var users = await _context.Users
    .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'")
    .ToListAsync();
```

---

## 7. Security Testing

### 7.1 Unit Tests

```csharp
[Fact]
public async Task Login_InvalidPassword_ShouldFail()
{
    var result = await _authService.AuthenticateAsync(new LoginRequest
    {
        Email = "user@example.com",
        Password = "WrongPassword"
    });
    
    Assert.False(result.Success);
    Assert.Equal("Invalid credentials", result.ErrorMessage);
}

[Fact]
public async Task GetDiscountRequest_UnauthorizedUser_ShouldThrow()
{
    var userId = Guid.NewGuid();  // Different company
    var requestId = existingRequest.Id;
    
    await Assert.ThrowsAsync<UnauthorizedException>(
        () => _useCase.ExecuteAsync(userId, requestId));
}
```

### 7.2 Integration Tests

```csharp
[Fact]
public async Task API_WithoutToken_ShouldReturn401()
{
    var response = await _client.GetAsync("/api/discount-requests");
    
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task API_WithExpiredToken_ShouldReturn401()
{
    var expiredToken = GenerateExpiredToken();
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", expiredToken);
    
    var response = await _client.GetAsync("/api/discount-requests");
    
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### 7.3 Security Checklist

Before production deployment:

- [ ] All API endpoints require authentication
- [ ] All API endpoints validate authorization
- [ ] All database queries filter by CompanyId
- [ ] JWT tokens are short-lived (15-60 min)
- [ ] Refresh tokens are stored in database
- [ ] Passwords are hashed with bcrypt/Argon2
- [ ] HTTPS only (no HTTP)
- [ ] Security headers configured
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention (output encoding)
- [ ] CSRF protection (if using cookies)
- [ ] Audit logging for all sensitive operations
- [ ] Multi-tenant isolation verified
- [ ] GDPR/LGPD compliance implemented
- [ ] Data encryption at rest and in transit
- [ ] Security tests passing
- [ ] Penetration testing completed

---

## 8. Production Security

### 8.1 Secrets Management

**Never commit secrets to source control**:

```bash
# .gitignore
appsettings.Production.json
*.pfx
*.key
.env
```

Use **Azure Key Vault**, **AWS Secrets Manager**, or similar:

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://marginiq-vault.vault.azure.net/"),
    new DefaultAzureCredential());

// Access secrets
var jwtSecret = builder.Configuration["JwtSecret"];
var dbConnectionString = builder.Configuration["ConnectionStrings:Default"];
```

### 8.2 Security Monitoring

**Log all security events**:
- Failed login attempts
- Unauthorized access attempts
- Token validation failures
- Permission check failures
- Data access across company boundaries

**Alert on suspicious activity**:
- Multiple failed logins (brute force)
- Unusual data access patterns
- API abuse (high request volume)
- Data exfiltration attempts

### 8.3 Incident Response

**Runbook for security incidents**:

1. **Detect**
   - Monitor alerts
   - Check logs
   - User reports

2. **Assess**
   - Scope of breach
   - Data affected
   - Users impacted

3. **Contain**
   - Revoke compromised tokens
   - Lock affected accounts
   - Block malicious IPs

4. **Eradicate**
   - Fix vulnerability
   - Patch systems
   - Update dependencies

5. **Recover**
   - Force password resets
   - Re-enable accounts
   - Verify system integrity

6. **Learn**
   - Post-mortem analysis
   - Update procedures
   - Improve monitoring

---

## Conclusion

MarginIQ implements **comprehensive security** following industry best practices:

1. ✅ **JWT Authentication** - Secure token-based authentication
2. ✅ **Role-Based Authorization** - 3 roles with clear permission matrix
3. ✅ **Multi-Tenant Isolation** - Complete data separation by company
4. ✅ **Audit Logging** - Full traceability of all operations
5. ✅ **GDPR/LGPD Compliance** - User rights fully implemented
6. ✅ **API Security** - HTTPS, CORS, rate limiting, input validation
7. ✅ **Data Protection** - Encryption at rest and in transit

**Security is not optional** - it's built into every layer of the system.
