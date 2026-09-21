# ArchiveCore Database

Target database: **`ArchiveCoreDb`**

## Current implementation

Phase 1 establishes the normalized relational foundation.

```text
database/
├── 01-schema.sql
├── 02-constraints.sql
├── 03-indexes.sql        # Phase 2
├── 04-views.sql          # Phase 2
├── 05-procedures.sql     # Phase 2
├── 06-triggers.sql       # Phase 2
└── 07-seed.sql
```

## Phase 1 entities

- Users
- Roles
- UserRoles
- Records
- RecordStatuses
- Documents
- DocumentVersions
- DocumentCategories
- RecordMovements
- MovementTypes
- AuditEvents

## Execution order

Run:

1. `01-schema.sql`
2. `02-constraints.sql`
3. `07-seed.sql`

Phase 2 will add performance indexes, views, stored procedures, triggers and advanced audit behavior.

## Design goals

- Third Normal Form operational model.
- Explicit referential integrity.
- Business-key uniqueness.
- SQL Server-friendly concurrency with `rowversion`.
- UTC timestamps.
- Version-aware document metadata.
- Traceable records workflow.
