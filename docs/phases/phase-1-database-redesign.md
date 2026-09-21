# Phase 1 — Database Redesign & Normalization

## Status

Implemented on branch `phase/1-database-redesign`.

## Deliverables

- Conceptual model.
- Legacy-to-modern entity mapping.
- 3NF normalization rationale.
- Initial physical design.
- SQL Server database creation script.
- Primary keys.
- Foreign keys.
- Unique constraints.
- Check constraints.
- Concurrency columns.
- Baseline catalog seed data.

## Main entities

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

## Deliberately deferred to Phase 2

The following are not Phase 1 omissions; they are intentionally postponed until query patterns are defined:

- Performance indexes.
- Views.
- Stored procedures.
- Triggers.
- Advanced audit automation.
- Reporting queries.
- Query-plan validation.

## Outcome

The unexplained legacy fields `IdArchivo` and `IdRegistro` are now modeled as real relational concepts: documents and records. The legacy administrator/user duplication is replaced with a normalized user-role model.
