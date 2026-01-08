# RBAC (Role-Based Access Control)

This document describes the implemented RBAC (3.1) for MarginIQ.

## Roles

- `Salesperson` – can create discount requests and view their own requests.
- `Manager` – can approve/reject discount requests, view company requests, and manage business rules.
- `Admin` – full system access, including AI configuration, user management and imports.

## Implementation

- Interface: `Application.Ports.IAuthorizationService`
- Implementation: `Application.Services.AuthorizationService`

The service uses repositories to evaluate permissions and to enforce multi-tenant isolation (company ownership).

## Key methods

- `HasRoleAsync(userId, role)` – check exact role
- `HasAnyRoleAsync(userId, roles)` – check membership in any provided roles
- `BelongsToCompanyAsync(userId, companyId)` – multi-tenant ownership check
- `CanApproveDiscountsAsync(userId)` – Manager or Admin and active
- `CanConfigureAIAsync(userId)` – Admin only
- `CanViewDiscountRequestAsync(userId, discountRequestId)` – Salesperson (own request) or Manager/Admin (company-wide)
- `CanEditDiscountRequestAsync(userId, discountRequestId)` – Salesperson editing own Draft/AdjustmentRequested only
- `GetUserPermissionsAsync(userId)` – returns `UserPermissions` with a permission set for UI rendering

## DI registration

The service is registered in `Api/Program.cs`:

```
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
```

## How to use

- Inject `IAuthorizationService` into application/use cases or controllers.
- Use `GetUserPermissionsAsync` to drive UI feature visibility.
- Always validate company ownership (`BelongsToCompanyAsync`) before returning tenant data.

## Notes

- The system is role-centric (one role per user). Business rules may further narrow permissions.
- For advanced scenarios (permissions per resource, dynamic scopes), extend `UserPermissions` and the service logic.
