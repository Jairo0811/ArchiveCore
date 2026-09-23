# Phase 3 — .NET Backend Foundation

## Objective

Create the .NET 10 backend foundation for ArchiveCore without coupling the domain model to ASP.NET Core, Entity Framework Core or SQL Server.

## Solution structure

```text
src/
├── ArchiveCore.Domain/
├── ArchiveCore.Application/
├── ArchiveCore.Infrastructure/
└── ArchiveCore.WebApi/
```

## Layer responsibilities

### Domain

Contains the core business entities and relationships. It has no dependency on EF Core or ASP.NET Core.

### Application

Reserved for use cases, contracts, validation and orchestration. Phase 3 establishes its dependency-registration entry point.

### Infrastructure

Contains:

- `ArchiveCoreDbContext`
- Entity Framework Core mappings
- SQL Server provider configuration
- Persistence dependency registration

### WebApi

Acts as the ASP.NET Core host and composition root.

Phase 3 includes:

- OpenAPI in Development.
- HTTPS redirection.
- Root service-status endpoint.
- Database-aware health check.
- Application and Infrastructure registration.

## Database strategy

The SQL scripts from Phases 1 and 2 remain the source of truth for the advanced database design.

EF Core maps to the existing `ArchiveCoreDb` schema instead of replacing the database-first work with a simplified code-first schema.

## Configuration

The API expects:

`ConnectionStrings:ArchiveCore`

Machine-specific credentials should be moved to .NET user-secrets or environment variables before deployment.

## Phase 4 boundary

Authentication, JWT, refresh tokens, password hashing, roles and authorization policies are intentionally deferred to Phase 4.
