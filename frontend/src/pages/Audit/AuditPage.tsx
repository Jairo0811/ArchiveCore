import { useState, type FormEvent } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { auditApi, type AuditFilters } from "../../api/endpoints";
import { PageState, QueryError } from "../../components/PageState";
import { Pagination } from "../../components/Pagination";
import { Tag } from "../../components/StatusTag";
import { UserSelect } from "../../components/UserSelect";
import { PageHeader } from "../../layout/Shell";
import { ACTION_LABELS, ENTITY_LABELS, actionLabel, entityLabel } from "../../utils/labels";
import { formatDateTime } from "../../utils/format";

type FormState = {
  entityName: string;
  entityKey: string;
  userId: number | null;
  actionType: string;
  from: string;
  to: string;
};

const EMPTY: FormState = { entityName: "", entityKey: "", userId: null, actionType: "", from: "", to: "" };
const PAGE_SIZE = 25;

/** Local calendar day -> UTC instant (inclusive start / exclusive end of the day). */
function dayStartUtc(value: string, addDays = 0): string | undefined {
  if (!value) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  return new Date(year, month - 1, day + addDays).toISOString();
}

function toFilters(form: FormState, page: number): AuditFilters {
  return {
    entityName: form.entityName || undefined,
    entityKey: form.entityKey.trim() || undefined,
    userId: form.userId,
    actionType: form.actionType || undefined,
    fromUtc: dayStartUtc(form.from),
    toUtc: dayStartUtc(form.to, 1),
    page,
    pageSize: PAGE_SIZE,
  };
}

const TONE: Record<string, "open" | "review" | "closed" | "danger"> = {
  INSERT: "open",
  UPDATE: "review",
  DELETE: "danger",
  DOWNLOAD: "closed",
  LOGIN: "closed",
  LOGOUT: "closed",
};

export function AuditPage() {
  const [form, setForm] = useState<FormState>(EMPTY);
  const [applied, setApplied] = useState<FormState>(EMPTY);
  const [page, setPage] = useState(1);
  const [dateError, setDateError] = useState<string>();

  const filters = toFilters(applied, page);
  const audit = useQuery({
    queryKey: ["audit", filters],
    queryFn: () => auditApi.search(filters),
    placeholderData: keepPreviousData,
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (form.from && form.to && form.from > form.to) {
      setDateError("La fecha inicial no puede ser posterior a la final.");
      return;
    }
    setDateError(undefined);
    setPage(1);
    setApplied(form);
  };

  const reset = () => {
    setForm(EMPTY);
    setApplied(EMPTY);
    setDateError(undefined);
    setPage(1);
  };

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => setForm((current) => ({ ...current, [key]: value }));
  const items = audit.data?.items ?? [];

  return (
    <>
      <PageHeader
        eyebrow="Trazabilidad"
        title="Auditoría"
        description="Eventos registrados por los triggers de SQL Server y por la API (inicio/cierre de sesión y descargas)."
      />

      <form className="filter-panel" onSubmit={submit} aria-label="Filtros de auditoría">
        <label className="filter-field">
          <span>Entidad</span>
          <select value={form.entityName} onChange={(event) => set("entityName", event.target.value)}>
            <option value="">Todas</option>
            {Object.entries(ENTITY_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <label className="filter-field">
          <span>Registro (ID)</span>
          <input value={form.entityKey} inputMode="numeric" maxLength={200} onChange={(event) => set("entityKey", event.target.value)} placeholder="Ej. 42" />
        </label>
        <label className="filter-field">
          <span>Usuario</span>
          <UserSelect value={form.userId} onChange={(value) => set("userId", value)} emptyLabel="Todos" />
        </label>
        <label className="filter-field">
          <span>Acción</span>
          <select value={form.actionType} onChange={(event) => set("actionType", event.target.value)}>
            <option value="">Todas</option>
            {Object.entries(ACTION_LABELS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select>
        </label>
        <label className="filter-field">
          <span>Desde</span>
          <input type="date" value={form.from} onChange={(event) => set("from", event.target.value)} aria-invalid={dateError ? true : undefined} />
        </label>
        <label className="filter-field">
          <span>Hasta</span>
          <input type="date" value={form.to} onChange={(event) => set("to", event.target.value)} aria-invalid={dateError ? true : undefined} />
        </label>
        <div className="filter-actions">
          <button type="submit" className="primary compact">Aplicar filtros</button>
          <button type="button" className="secondary compact" onClick={reset}>Restablecer</button>
        </div>
        {dateError && <p className="form-error" role="alert">{dateError}</p>}
      </form>

      <section className="table-card" aria-busy={audit.isFetching}>
        {audit.isLoading && <PageState kind="loading" text="Cargando auditoría…" />}
        {audit.error && <QueryError error={audit.error} onRetry={() => audit.refetch()} text="No se pudo cargar la auditoría" />}
        {audit.data && (
          <>
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Eventos de auditoría</caption>
                <thead>
                  <tr><th scope="col">Fecha</th><th scope="col">Entidad</th><th scope="col">Registro</th><th scope="col">Acción</th><th scope="col">Usuario</th><th scope="col">Correlación</th></tr>
                </thead>
                <tbody>
                  {items.map((event) => (
                    <tr key={event.auditEventId}>
                      <td>{formatDateTime(event.createdAtUtc)}</td>
                      <td>{entityLabel(event.entityName)}</td>
                      <td className="mono">{event.entityKey}</td>
                      <td><Tag tone={TONE[event.actionType] ?? "open"}>{actionLabel(event.actionType)}</Tag></td>
                      <td>{event.userName ?? <span className="muted-text">Sistema</span>}</td>
                      <td className="mono hash" title={event.correlationId ?? undefined}>{event.correlationId ? `${event.correlationId.slice(0, 8)}…` : "—"}</td>
                    </tr>
                  ))}
                  {items.length === 0 && <tr><td colSpan={6} className="empty">No hay eventos que coincidan con los filtros.</td></tr>}
                </tbody>
              </table>
            </div>
            <Pagination page={audit.data.page} totalPages={audit.data.totalPages} totalCount={audit.data.totalCount} label="eventos" onPageChange={setPage} />
          </>
        )}
      </section>
    </>
  );
}
