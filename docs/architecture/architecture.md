# ArchiveCore architecture direction

## Goal

Transform the legacy database exercise into a modular document and records management platform while keeping SQL Server database design as a core engineering concern.

## Initial bounded areas

### Identity
Users, roles and permissions.

### Records
Case files / records that group documents and business activity.

### Documents
Document metadata, versions, categories and storage references.

### Workflow
Record assignments, movements, statuses and lifecycle transitions.

### Audit
Immutable trace of important business and security changes.

### Reporting
Operational views and database-backed reporting queries.

## Dependency direction

The backend will use an Onion / Clean Architecture dependency flow:

```text
WebApi
  ↓
Application
  ↓
Domain

Infrastructure ──> Application / Domain
```

Domain code must not depend on Entity Framework Core, SQL Server or ASP.NET Core.

## Data principles

- SQL Server is the authoritative relational store.
- Referential integrity is enforced by constraints.
- Business identifiers receive explicit uniqueness rules.
- Frequently queried access paths receive justified indexes.
- Critical changes receive an auditable trail.
- Stored procedures and triggers are used where they add database-level value rather than as arbitrary requirements.
- Application and database validation complement each other.
