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
