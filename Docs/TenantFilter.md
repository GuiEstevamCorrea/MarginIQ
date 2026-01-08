# Tenant Filter (Multi-Tenancy Isolation)

This document describes the multi-tenant isolation filter (3.2) implemented in the application.

## Goal

Ensure that requests only access data belonging to the authenticated user's company (tenant) and block cross-tenant access.

## Approach

- Middleware: `Api.Middleware.TenantMiddleware` extracts `CompanyId` from JWT claims and validates it.
- DbContext-level filter: `Infrastructure.Data.MarginIQDbContext` exposes a per-request property `CurrentCompanyId`.
- Global query filter: `MarginIQDbContext` applies a `HasQueryFilter` to every entity that contains a `CompanyId` property. The filter compares the entity `CompanyId` to the DbContext `CurrentCompanyId`.

## How it works (runtime)

1. The request arrives with a valid JWT containing `CompanyId` and `sub` (user id).
2. `TenantMiddleware` validates the claims and writes tenant info into `HttpContext.Items` and sets `MarginIQDbContext.CurrentCompanyId` for the scoped DbContext instance.
3. All EF Core queries executed through that DbContext instance automatically include the `WHERE CompanyId = {CurrentCompanyId}` filter.

## Important details

- Entities are not required to implement any interface. The filter is applied automatically to all entities that have a `CompanyId` property in the model.
- If `CurrentCompanyId` remains `Guid.Empty` (e.g. unauthenticated endpoints), the filter will match entities with `CompanyId == Guid.Empty` — which normally yields no results because real companies use non-empty GUIDs. Authentication endpoints are skipped by the middleware.
- The filter protects read operations; repository write methods should also set `CompanyId` when creating entities (existing repositories already follow this rule).

## How to test

Manual test:

1. Create two companies and users for each via seed data or API.
2. Authenticate as user A (Company A) and fetch `/api/discount-requests` — only Company A requests must appear.
3. Authenticate as user B (Company B) and repeat — only Company B requests must appear.

Automated test idea:

- Add integration tests that seed two companies and run queries with two separate authenticated HttpClient instances (different JWTs). Assert separation of results.

## Files changed

- `Infrastructure/Data/MarginIQDbContext.cs` — added `CurrentCompanyId` and global query filter
- `Api/Middleware/TenantMiddleware.cs` — now sets `MarginIQDbContext.CurrentCompanyId` per request
