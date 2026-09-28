# Legacy database analysis

## Source

Original artifact: `DatabaseProject.sql`

The original database was named `DataBaseProject` / `DatabaseProject` and defined two tables.

## Administrador

Columns:

- `IdAdmin` — primary key.
- `IdArchivo` — required integer.
- `Nombre`
- `Apellido`
- `Cedula`
- `Telefono`

## Usuarios

Columns:

- `IdUser` — identity primary key.
- `IdRegistro` — required integer.
- `Nombre`
- `Apellido`
- `Sexo`
- `Cedula`
- `FechaNacimiento`

## What the legacy model suggests

The names `IdArchivo` and `IdRegistro` indicate an early intent to associate administrators and users with files or records. The original script, however, does not define the corresponding entities or relationships.

This gap is the foundation for ArchiveCore's modernization into a document and records management system.

## Technical limitations of the legacy script

The legacy artifact does not yet contain:

- Foreign keys.
- Unique constraints.
- Check constraints.
- Relationship tables.
- Explicit indexes beyond primary keys.
- Stored procedures.
- Triggers.
- Views.
- Audit tables.
- Transactions.
- Record/document entities.
- Authentication or authorization model.

Some data types also need modernization. For example, telephone numbers should not be numeric values because they are identifiers rather than quantities.

## Preservation rule

`DatabaseProject-original.sql` is historical material.

It should remain unchanged conceptually and should not be used directly as the production schema. New database work belongs under `/database`.


## Canonical source reconciliation

The repository copy at `docs/legacy/DatabaseProject-original.sql` was reconciled against the original `DatabaseProject.sql` source supplied during the 2026 reconstruction.

The SQL structure and academic content match the original artifact. The repository stores the text as UTF-8 for modern tooling compatibility.

The SQL Lab treats this file as the canonical legacy source for the original read queries:

- `SELECT * FROM Administrador`
- `SELECT * FROM Usuarios`

The historical `INSERT` statements are preserved only in the legacy artifact. Their identity/contact values are intentionally not rendered by the modern application's SQL Lab.


## Public release note

The canonical legacy artifact is preserved for historical accuracy inside the private repository. Before making the repository public, review the historical `INSERT` statements and publish a sanitized legacy copy if necessary so identity/contact data from the original academic artifact is not exposed.
