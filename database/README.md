# ArchiveCore Database

The production database will be designed during **Phase 1 — Database Redesign & Normalization**.

Planned script organization:

```text
database/
├── 01-schema.sql
├── 02-constraints.sql
├── 03-indexes.sql
├── 04-views.sql
├── 05-procedures.sql
├── 06-triggers.sql
└── 07-seed.sql
```

## Target database

`ArchiveCoreDb`

## Database responsibilities

The redesigned schema will cover:

- Users and roles.
- Records / case files.
- Documents.
- Document categories.
- Record statuses.
- Record movements.
- Document versions.
- Audit events.

The final design will include normalization, referential integrity, performance indexes, stored procedures, views and audit triggers.
