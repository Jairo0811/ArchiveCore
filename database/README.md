# ArchiveCore Database

Target database: **`ArchiveCoreDb`**

## Current implementation

```text
database/
├── 01-schema.sql
├── 02-constraints.sql
├── 03-indexes.sql
├── 04-views.sql
├── 05-procedures.sql
├── 06-triggers.sql
├── 07-seed.sql
└── 08-queries.sql
```

## Execution order

1. `01-schema.sql`
2. `02-constraints.sql`
3. `07-seed.sql`
4. `03-indexes.sql`
5. `04-views.sql`
6. `05-procedures.sql`
7. `06-triggers.sql`
8. `08-queries.sql` — optional example queries

## Advanced SQL capabilities

Phase 2 adds:

- Query-driven nonclustered indexes.
- Filtered unique index for one current document version.
- Operational/reporting views.
- Transactional stored procedures.
- SQL Server `SESSION_CONTEXT` actor propagation.
- JSON audit snapshots.
- Audit triggers for critical entities.
- Representative reporting queries.

## Design goals

- Third Normal Form operational model.
- Explicit referential integrity.
- Business-key uniqueness.
- SQL Server-friendly optimistic concurrency.
- UTC timestamps.
- Version-aware documents.
- Traceable workflow.
- Database-level auditability.
