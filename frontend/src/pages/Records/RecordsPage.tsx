import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { recordsApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { can } from "../../auth/permissions";
import { Icon } from "../../components/Icon";
import { PageState, QueryError } from "../../components/PageState";
import { Pagination } from "../../components/Pagination";
import { StatusTag } from "../../components/StatusTag";
import { useUserLookup } from "../../hooks/useUserLookup";
import { useDebouncedValue } from "../../hooks/useDebouncedValue";
import { PageHeader } from "../../layout/Shell";
import { formatDate } from "../../utils/format";
import { CreateRecordModal } from "./RecordFormModal";
import { useRecordStatuses } from "./recordQueries";

const PAGE_SIZE = 20;

export function RecordsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [creating, setCreating] = useState(false);
  const debouncedSearch = useDebouncedValue(search.trim());

  const statusId = params.get("status") ? Number(params.get("status")) : null;
  const assignee = params.get("assignee") ?? "";
  const page = Math.max(1, Number(params.get("page") ?? "1") || 1);

  const statuses = useRecordStatuses();
  const users = useUserLookup();

  const filters = {
    search: debouncedSearch || undefined,
    statusId,
    assignedToUserId: assignee && assignee !== "none" ? Number(assignee) : null,
    unassigned: assignee === "none" ? true : undefined,
    page,
    pageSize: PAGE_SIZE,
  };

  const records = useQuery({
    queryKey: ["records", "list", filters],
    queryFn: () => recordsApi.list(filters),
    placeholderData: keepPreviousData,
  });

  const updateParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.delete("page");
    setParams(next, { replace: true });
  };

  const hasFilters = !!(search || statusId || assignee);
  const items = records.data?.items ?? [];

  return (
    <>
      <PageHeader
        eyebrow="Gestión documental"
        title="Expedientes"
        actions={can.manageRecords(user) && (
          <button type="button" className="primary compact" onClick={() => setCreating(true)}>
            <Icon name="plus" size={18} /> Nuevo expediente
          </button>
        )}
      />

      <section className="filter-bar" aria-label="Filtros de expedientes">
        <label className="search-input-wrap">
          <Icon name="search" />
          <span className="visually-hidden">Buscar por código o título</span>
          <input
            className="search"
            type="search"
            placeholder="Buscar código o título…"
            value={search}
            maxLength={200}
            onChange={(event) => {
              setSearch(event.target.value);
              updateParam("search", event.target.value.trim() || null);
            }}
          />
        </label>
        <label className="filter-field">
          <span>Estado</span>
          <select value={statusId ?? ""} onChange={(event) => updateParam("status", event.target.value || null)}>
            <option value="">Todos</option>
            {(statuses.data ?? []).map((status) => (
              <option key={status.recordStatusId} value={status.recordStatusId}>{status.name}</option>
            ))}
          </select>
        </label>
        <label className="filter-field">
          <span>Responsable</span>
          <select value={assignee} onChange={(event) => updateParam("assignee", event.target.value || null)}>
            <option value="">Todos</option>
            <option value="none">Sin asignar</option>
            {(users.data ?? []).map((item) => (
              <option key={item.userId} value={item.userId}>{item.fullName}</option>
            ))}
          </select>
        </label>
        {hasFilters && (
          <button type="button" className="secondary compact" onClick={() => { setSearch(""); setParams({}, { replace: true }); }}>
            Limpiar filtros
          </button>
        )}
      </section>

      <section className="table-card" aria-busy={records.isFetching}>
        {records.isLoading && <PageState kind="loading" text="Cargando expedientes…" />}
        {records.error && <QueryError error={records.error} onRetry={() => records.refetch()} text="No se pudieron cargar los expedientes" />}
        {records.data && (
          <>
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Listado de expedientes</caption>
                <thead>
                  <tr>
                    <th scope="col">Código</th>
                    <th scope="col">Título</th>
                    <th scope="col">Estado</th>
                    <th scope="col">Responsable</th>
                    <th scope="col">Apertura</th>
                    <th scope="col">Cierre</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((record) => (
                    <tr key={record.recordId} className="clickable" onClick={() => navigate(`/records/${record.recordId}`)}>
                      <td className="mono"><Link to={`/records/${record.recordId}`} onClick={(event) => event.stopPropagation()}>{record.recordNumber}</Link></td>
                      <td className="wrap">{record.title}</td>
                      <td><StatusTag code={record.statusCode} label={record.status} /></td>
                      <td>{record.assignedToName ?? <span className="muted-text">Sin asignar</span>}</td>
                      <td>{formatDate(record.openedAtUtc)}</td>
                      <td>{formatDate(record.closedAtUtc)}</td>
                    </tr>
                  ))}
                  {items.length === 0 && (
                    <tr>
                      <td colSpan={6} className="empty">
                        {hasFilters ? "Ningún expediente coincide con los filtros." : "Todavía no hay expedientes registrados."}
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <Pagination
              page={records.data.page}
              totalPages={records.data.totalPages}
              totalCount={records.data.totalCount}
              label="expedientes"
              onPageChange={(next) => updateParam("page", String(next))}
            />
          </>
        )}
      </section>

      {creating && (
        <CreateRecordModal
          onClose={() => setCreating(false)}
          onCreated={(record) => navigate(`/records/${record.recordId}`)}
        />
      )}
    </>
  );
}
