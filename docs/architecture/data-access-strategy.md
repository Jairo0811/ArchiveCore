# Data access strategy: stored procedures and EF Core

ArchiveCore keeps **one source of truth per operation**. No business rule is implemented twice
with different behaviour.

## Decision

| Operation | Implementation | Why |
|---|---|---|
| Create record (+ initial assignment movement) | `dbo.sp_CreateRecord` | Transactional rule designed in Phase 2: record, initial `ASSIGN` movement and audit in one transaction. |
| Add document version | `dbo.sp_AddDocumentVersion` | Serialises concurrent uploads, demotes the previous current version and keeps the filtered unique index (one current version per document) as the final guard. |
| Assign / transfer record | `dbo.sp_TransferRecord` | `ASSIGN` for an unassigned record, `TRANSFER` otherwise; validates the active target user, rejects closed records and same-user transfers, writes the movement atomically. |
| Close record | `dbo.sp_CloseRecord` | Rejects a second close, sets the final status and writes the `CLOSE` movement atomically. |
| Queries, paging, filters, catalogs, dashboard, audit search | EF Core (LINQ, no tracking) | Composable read models; no business invariants. |
| Record metadata update (title, description, status) | EF Core with `rowversion` | Optimistic concurrency → HTTP 409 on conflict. |
| Document metadata creation, logical delete, users, refresh tokens | EF Core | Simple writes whose invariants are enforced by constraints. |

The API calls the four procedures through `StoredProcedures.ExecuteScalarAsync`, on the same
connection and transaction as the `DbContext`. SQL errors raised by the procedures
(`THROW 50xxx`) are translated by `SqlErrorTranslator` into 400/403/404/409 `ProblemDetails`;
internal SQL messages are never returned to the client.

The academic SQL scripts (`database/05-procedures.sql`) therefore are not a parallel
implementation: they **are** the implementation the application executes, and the SQL Server
integration tests exercise them on a clean database.

## Audit actor attribution

The audit triggers read `SESSION_CONTEXT('UserId')`, `SESSION_CONTEXT('IpAddress')` and
`SESSION_CONTEXT('CorrelationId')`.

`AuditSessionContextInterceptor` (an EF Core `DbConnectionInterceptor`) writes the three keys
every time EF Core opens a connection, for EF Core commands and stored procedures alike. All
three keys are written on every open, including explicit `NULL`s, so a pooled physical
connection can never carry the actor of a previous request. Anonymous operations (for example
a failed login) are attributed to no user.

The integration test `AuditActorTests` verifies that when user A creates a record and user B
modifies it, the audit event of the modification identifies user B.
