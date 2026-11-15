#!/usr/bin/env bash
# End-to-end smoke test of the Docker Compose stack through Nginx (same origin as the SPA).
# Expects the stack to be up and .env to contain the bootstrap administrator credentials.
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:${ARCHIVECORE_WEB_PORT:-5173}}"
ADMIN_EMAIL="${ARCHIVECORE_BOOTSTRAP_ADMIN_EMAIL:?}"
ADMIN_PASSWORD="${ARCHIVECORE_BOOTSTRAP_ADMIN_PASSWORD:?}"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

pass() { printf '  PASS  %s\n' "$1"; }
fail() { printf '  FAIL  %s\n' "$1" >&2; exit 1; }
json() { python3 -c "import json,sys; d=json.load(sys.stdin); print(eval(sys.argv[1], {}, {'d': d}))" "$1"; }

echo "Waiting for $BASE_URL/health ..."
for attempt in $(seq 1 60); do
  if [ "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/health")" = "200" ]; then break; fi
  [ "$attempt" -eq 60 ] && fail "API health through Nginx"
  sleep 3
done
pass "API health through Nginx"

headers="$(curl -s -D - -o /dev/null "$BASE_URL/")"
grep -qi '^content-security-policy:' <<< "$headers" || fail "SPA served with CSP"
grep -qi '^x-content-type-options: nosniff' <<< "$headers" || fail "SPA served with nosniff"
curl -s "$BASE_URL/" | grep -q '<div id="root">' || fail "SPA index"
pass "SPA index with security headers"

[ "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/v1/records")" = "401" ] || fail "Anonymous request rejected"
pass "Anonymous request rejected (401)"

login() { # email password cookiejar -> access token
  curl -s -c "$3" -b "$3" -H 'Content-Type: application/json' \
    -d "{\"email\":\"$1\",\"password\":\"$2\"}" "$BASE_URL/api/v1/auth/login" | json "d['accessToken']"
}

ADMIN_TOKEN="$(login "$ADMIN_EMAIL" "$ADMIN_PASSWORD" "$WORK/admin.jar")"
[ -n "$ADMIN_TOKEN" ] || fail "Administrator login"
pass "Administrator login (bootstrap)"

REFRESHED="$(curl -s -c "$WORK/admin.jar" -b "$WORK/admin.jar" -X POST "$BASE_URL/api/v1/auth/refresh" | json "d['accessToken']")"
[ -n "$REFRESHED" ] || fail "Refresh through HttpOnly cookie"
ADMIN_TOKEN="$REFRESHED"
pass "Refresh through HttpOnly cookie"

auth() { curl -s -H "Authorization: Bearer $1" "${@:2}"; }

MANAGER_EMAIL="manager.$(date +%s)@archivecore.test"
MANAGER_PASSWORD="Mgr1!$(openssl rand -hex 12)"
status="$(auth "$ADMIN_TOKEN" -o "$WORK/user.json" -w '%{http_code}' -H 'Content-Type: application/json' \
  -d "{\"firstName\":\"Gestora\",\"lastName\":\"Smoke\",\"email\":\"$MANAGER_EMAIL\",\"password\":\"$MANAGER_PASSWORD\",\"roles\":[\"RecordsManager\"]}" \
  "$BASE_URL/api/v1/users")"
[ "$status" = "201" ] || fail "Create RecordsManager ($status)"
MANAGER_ID="$(json "d['userId']" < "$WORK/user.json")"
pass "Administrator creates RecordsManager"

MANAGER_TOKEN="$(login "$MANAGER_EMAIL" "$MANAGER_PASSWORD" "$WORK/manager.jar")"
[ "$(auth "$MANAGER_TOKEN" -o /dev/null -w '%{http_code}' "$BASE_URL/api/v1/academic/sql-lab/queries")" = "403" ] || fail "SQL Lab is admin-only"
pass "RBAC: RecordsManager gets 403 on SQL Lab"

NUMBER="SMOKE-$(date +%s)"
status="$(auth "$MANAGER_TOKEN" -o "$WORK/record.json" -w '%{http_code}' -H 'Content-Type: application/json' \
  -d "{\"recordNumber\":\"$NUMBER\",\"title\":\"Expediente smoke Docker\"}" "$BASE_URL/api/v1/records")"
[ "$status" = "201" ] || fail "Create record ($status)"
RECORD_ID="$(json "d['recordId']" < "$WORK/record.json")"
pass "Create record"

status="$(auth "$MANAGER_TOKEN" -o "$WORK/doc.json" -w '%{http_code}' -H 'Content-Type: application/json' \
  -d "{\"recordId\":$RECORD_ID,\"documentCategoryId\":1,\"title\":\"Documento smoke\"}" "$BASE_URL/api/v1/documents")"
[ "$status" = "201" ] || fail "Create document ($status)"
DOCUMENT_ID="$(json "d['documentId']" < "$WORK/doc.json")"
pass "Create document metadata"

# 5 MB PDF-signed payload: larger than Nginx's default 1 MB body limit, below the API limit.
{ printf '%%PDF-1.7\n'; head -c 5242880 /dev/urandom; printf '\n%%%%EOF\n'; } > "$WORK/sample.pdf"
EXPECTED_HASH="$(sha256sum "$WORK/sample.pdf" | cut -d' ' -f1 | tr '[:lower:]' '[:upper:]')"
status="$(auth "$MANAGER_TOKEN" -o "$WORK/version.json" -w '%{http_code}' -F "file=@$WORK/sample.pdf;type=application/pdf" \
  "$BASE_URL/api/v1/documents/$DOCUMENT_ID/versions")"
[ "$status" = "200" ] || fail "Upload 5 MB version through Nginx ($status: $(cat "$WORK/version.json"))"
[ "$(json "d['versions'][0]['sha256Hash'].strip()" < "$WORK/version.json")" = "$EXPECTED_HASH" ] || fail "Stored SHA-256"
pass "Upload 5 MB version through Nginx (SHA-256 verified)"

auth "$MANAGER_TOKEN" -o "$WORK/download.pdf" "$BASE_URL/api/v1/documents/$DOCUMENT_ID/download"
cmp -s "$WORK/sample.pdf" "$WORK/download.pdf" || fail "Download matches upload"
pass "Download matches upload"

[ "$(auth "$MANAGER_TOKEN" -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' \
  -d "{\"toUserId\":$MANAGER_ID,\"notes\":\"Asignación smoke\"}" "$BASE_URL/api/v1/records/$RECORD_ID/workflow/transfer")" = "204" ] || fail "Assign record"
[ "$(auth "$MANAGER_TOKEN" -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' \
  -d '{"notes":"Cierre smoke"}' "$BASE_URL/api/v1/records/$RECORD_ID/workflow/close")" = "204" ] || fail "Close record"
[ "$(auth "$MANAGER_TOKEN" -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' \
  -d '{}' "$BASE_URL/api/v1/records/$RECORD_ID/workflow/close")" = "409" ] || fail "Second close rejected"
pass "Workflow assign -> close (second close 409)"

auth "$ADMIN_TOKEN" "$BASE_URL/api/v1/audit?entityName=Records&entityKey=$RECORD_ID&pageSize=50" > "$WORK/audit.json"
actors="$(json "sorted({e['userId'] for e in d['items']})" < "$WORK/audit.json")"
[ "$actors" = "[$MANAGER_ID]" ] || fail "Audit actor attribution (got $actors)"
pass "Audit events attributed to the acting user"

[ "$(auth "$ADMIN_TOKEN" -o /dev/null -w '%{http_code}' "$BASE_URL/api/v1/academic/sql-lab/queries/legacy-admins")" = "200" ] || fail "SQL Lab for Administrator"
pass "SQL Lab for Administrator"

cp "$WORK/admin.jar" "$WORK/admin-before-logout.jar"
[ "$(curl -s -b "$WORK/admin.jar" -c "$WORK/admin.jar" -o /dev/null -w '%{http_code}' -X POST "$BASE_URL/api/v1/auth/revoke")" = "204" ] || fail "Revoke"
[ "$(curl -s -b "$WORK/admin-before-logout.jar" -o /dev/null -w '%{http_code}' -X POST "$BASE_URL/api/v1/auth/refresh")" = "401" ] || fail "Refresh with revoked token"
pass "Logout revokes the refresh token"

echo "Docker smoke test passed."
