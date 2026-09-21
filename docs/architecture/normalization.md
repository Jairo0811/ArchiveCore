# Normalization rationale

ArchiveCore Phase 1 targets a relational model consistent with **Third Normal Form (3NF)** for the operational schema.

## 1NF

All modeled columns are atomic.

Examples:

- User roles are not stored as comma-separated text; they use `UserRoles`.
- Document versions are individual rows in `DocumentVersions`.
- Record movements are individual rows in `RecordMovements`.

## 2NF

Tables with composite keys contain attributes dependent on the complete key.

The principal example is `UserRoles(UserId, RoleId)`. Assignment metadata describes that exact user-role association.

## 3NF

Descriptive catalog information is separated from transactional entities.

Examples:

- Record status names are stored in `RecordStatuses`, not repeated in `Records`.
- Document category names are stored in `DocumentCategories`, not repeated in `Documents`.
- Movement type descriptions are stored in `MovementTypes`, not repeated in `RecordMovements`.
- Role names are stored in `Roles`, not repeated in `Users`.

## Intentional design decisions

### Administrator is not a separate person table

The legacy `Administrador` table duplicates person data also found in `Usuarios`. ArchiveCore models an administrator as a `User` assigned an administrative `Role`. This removes duplication and prevents divergent identity records.

### File metadata is versioned

A logical document and a physical uploaded file are different concepts. `Documents` stores the logical document; `DocumentVersions` stores version-specific file metadata.

### Phone is text, not numeric

Telephone numbers are identifiers and may contain leading zeroes, country codes or formatting symbols. They are therefore represented as character data.

### NationalId is not a numeric quantity

Identification numbers are stored as text for the same reason and to avoid arithmetic semantics.

### Audit is cross-cutting

`AuditEvents` is intentionally generic because audit data describes changes across multiple entities. Phase 2 will define trigger strategy and application-level event strategy.
