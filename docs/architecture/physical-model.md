# Physical model notes

## Database

`ArchiveCoreDb`

## Schemas

Phase 1 uses the default `dbo` schema to keep the academic SQL artifact easy to inspect. A future hardening phase may separate bounded areas into dedicated SQL schemas if that provides operational value.

## Key strategy

- Surrogate integer/bigint keys for internal identity.
- Explicit business keys where required:
  - `Users.Email`
  - `Records.RecordNumber`
  - catalog `Code` fields
- Composite uniqueness for `DocumentVersions(DocumentId, VersionNumber)`.

## Concurrency

`Users`, `Records` and `Documents` include SQL Server `rowversion` columns. These support optimistic concurrency in the future .NET backend.

## Time

Operational timestamps are stored in UTC using `datetime2` and `SYSUTCDATETIME()`.

## Deletion strategy

Documents use `IsDeleted` for logical deletion. Records are lifecycle-driven and are closed using status plus `ClosedAtUtc` rather than physically deleted.

## Phase 2 boundary

Indexes beyond constraint-created indexes, views, stored procedures and audit triggers are deliberately deferred to Phase 2 so they can be justified by concrete query patterns.
