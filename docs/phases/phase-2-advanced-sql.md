# Phase 2 — Advanced SQL Server

## Objective

Implement the database-level capabilities explicitly emphasized by the original SOF-008 assignment: indexing, stored procedures, triggers and audit tracking.

## Deliverables

### Performance indexes

Indexes are based on expected access patterns rather than being added indiscriminately.

Key patterns include:

- Active users ordered by name.
- Records filtered by status and opening date.
- Records assigned to a specific user.
- Documents inside a record.
- Current document version lookup.
- Chronological movement history.
- Entity/user audit history.

A filtered unique index guarantees that a logical document has at most one current version.

### Views

- `vw_ActiveRecords`
- `vw_RecordDocumentSummary`
- `vw_CurrentDocumentVersions`
- `vw_RecordMovementHistory`
- `vw_AuditOverview`

### Stored procedures

- `sp_CreateRecord`
- `sp_AddDocumentVersion`
- `sp_TransferRecord`
- `sp_CloseRecord`
- `sp_SearchRecords`
- `sp_GetRecordHistory`
- `sp_GetEntityAudit`

Mutation procedures use explicit transactions and `SET XACT_ABORT ON`.

### Audit triggers

- `TR_Records_Audit`
- `TR_Documents_Audit`
- `TR_DocumentVersions_Audit`
- `TR_Users_Audit`

Audit snapshots are serialized as JSON into `AuditEvents`.

The application or stored-procedure layer can identify the acting user through SQL Server `SESSION_CONTEXT('UserId')`.

### Advanced query examples

`08-queries.sql` contains representative operational and reporting queries.

## Execution order

After Phase 1:

1. `03-indexes.sql`
2. `04-views.sql`
3. `05-procedures.sql`
4. `06-triggers.sql`
5. `08-queries.sql` (examples; optional)

## Academic alignment

This phase directly implements the original requirement that important entities have audit tracking using database mechanisms such as triggers and stored procedures, while also adding indexes for expected query performance.
