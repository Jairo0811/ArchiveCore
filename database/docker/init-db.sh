#!/usr/bin/env bash
# Initializes ArchiveCoreDb on a clean SQL Server volume and validates it.
# Runs as the one-shot "db-init" Compose service after "db" reports healthy.
set -euo pipefail

SQLCMD="$(command -v sqlcmd || true)"
for candidate in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
  [ -z "$SQLCMD" ] && [ -x "$candidate" ] && SQLCMD="$candidate"
done
[ -n "$SQLCMD" ] || { echo "sqlcmd was not found in the image." >&2; exit 1; }

: "${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
DB_HOST="${DB_HOST:-db}"
SCRIPTS_DIR="${SCRIPTS_DIR:-/database}"

# -b: abort on error, -I: QUOTED_IDENTIFIER ON (required by filtered indexes), -C: trust the dev certificate.
run() { SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" "$SQLCMD" -S "$DB_HOST" -U sa -C -b -I "$@"; }

# "db" is already healthy (depends_on: service_healthy); this bounded check only guards restarts.
for attempt in $(seq 1 30); do
  if run -Q "SET NOCOUNT ON; SELECT 1" -h -1 > /dev/null 2>&1; then break; fi
  [ "$attempt" -eq 30 ] && { echo "SQL Server is not reachable at $DB_HOST." >&2; exit 1; }
  sleep 2
done

initialized="$(run -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'ArchiveCoreDb.dbo.Users', N'U') IS NULL THEN 0 ELSE 1 END" | tr -d '[:space:]')"

if [ "$initialized" = "1" ]; then
  echo "ArchiveCoreDb already exists; skipping creation and running validation only."
else
  echo "Creating ArchiveCoreDb..."
  for script in 01-schema.sql 02-constraints.sql 07-seed.sql 03-indexes.sql 04-views.sql 05-procedures.sql 06-triggers.sql 09-auth.sql; do
    echo "  -> $script"
    run -i "$SCRIPTS_DIR/$script" > /dev/null
  done
fi

echo "  -> 10-validation.sql"
if ! output="$(run -i "$SCRIPTS_DIR/10-validation.sql" 2>&1)"; then
  echo "$output" >&2
  echo "Validation failed. If a previous initialization was interrupted, reset with: docker compose down -v" >&2
  exit 1
fi

echo "$output"
grep -q "ArchiveCore database validation passed." <<< "$output"
