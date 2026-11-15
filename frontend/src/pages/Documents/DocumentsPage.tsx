import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import { documentsApi, recordsApi } from "../../api/endpoints";
import type { RecordSummary } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { can } from "../../auth/permissions";
import { Icon } from "../../components/Icon";
import { PageState, QueryError } from "../../components/PageState";
import { StatusTag } from "../../components/StatusTag";
import { useDebouncedValue } from "../../hooks/useDebouncedValue";
import { PageHeader } from "../../layout/Shell";
import { formatDate } from "../../utils/format";
import { CreateDocumentModal } from "./CreateDocumentModal";

export function DocumentsPage() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const selectedId = Number(params.get("recordId")) || null;
  const [search, setSearch] = useState("");
  const [creating, setCreating] = useState(false);
  const debounced = useDebouncedValue(search.trim());

  const candidates = useQuery({
    queryKey: ["records", "list", { search: debounced, pageSize: 8, picker: true }],
    queryFn: () => recordsApi.list({ search: debounced || undefined, page: 1, pageSize: 8 }),
    enabled: !selectedId,
  });

  const selected = useQuery({
    queryKey: ["records", "detail", selectedId],
    queryFn: () => recordsApi.get(selectedId!),
    enabled: !!selectedId,
  });

  const documents = useQuery({
    queryKey: ["documents", "record", selectedId],
    queryFn: () => documentsApi.listByRecord(selectedId!),
    enabled: !!selectedId,
  });

  const choose = (record: RecordSummary) => setParams({ recordId: String(record.recordId) });
  const record = selected.data;

  return (
    <>
      <PageHeader
        eyebrow="Gestión documental"
        title="Documentos"
        description="Selecciona un expediente por código o título para consultar y gestionar sus documentos."
        actions={record && can.manageRecords(user) && !record.isFinal && (
          <button type="button" className="primary compact" onClick={() => setCreating(true)}>
            <Icon name="plus" size={18} /> Nuevo documento
          </button>
        )}
      />

      {!selectedId && (
        <section className="records-panel" aria-labelledby="record-picker-title">
          <div className="records-panel-head">
            <div>
              <h3 id="record-picker-title">Seleccionar expediente</h3>
              <span>Busca por código o título</span>
            </div>
          </div>
          <div className="records-toolbar">
            <label className="search-shell">
              <Icon name="search" />
              <span className="visually-hidden">Buscar expediente</span>
              <input type="search" value={search} maxLength={200} autoFocus
                onChange={(event) => setSearch(event.target.value)} placeholder="Ej. EXP-2026 o Licencia…" />
            </label>
          </div>
          <div className="table-card dashboard-table">
            {candidates.isLoading && <PageState kind="loading" text="Buscando expedientes…" />}
            {candidates.error && <QueryError error={candidates.error} onRetry={() => candidates.refetch()} />}
            {candidates.data && (
              <ul className="picker-list" aria-label="Expedientes encontrados">
                {candidates.data.items.map((item) => (
                  <li key={item.recordId}>
                    <button type="button" onClick={() => choose(item)}>
                      <span className="mono">{item.recordNumber}</span>
                      <span className="picker-title">{item.title}</span>
                      <StatusTag code={item.statusCode} label={item.status} />
                    </button>
                  </li>
                ))}
                {candidates.data.items.length === 0 && <li className="empty">No hay expedientes que coincidan.</li>}
              </ul>
            )}
          </div>
        </section>
      )}

      {selectedId && (
        <>
          <div className="selected-record">
            {selected.isLoading && <PageState kind="loading" text="Cargando expediente…" />}
            {selected.error && <QueryError error={selected.error} />}
            {record && (
              <>
                <div>
                  <span className="mono">{record.recordNumber}</span>
                  <strong>{record.title}</strong>
                  <StatusTag code={record.statusCode} label={record.status} />
                </div>
                <div className="inline-tools">
                  <Link className="see-all" to={`/records/${record.recordId}`}>Ver expediente</Link>
                  <button type="button" className="secondary compact" onClick={() => setParams({})}>Cambiar expediente</button>
                </div>
              </>
            )}
          </div>

          <section className="table-card">
            {documents.isLoading && <PageState kind="loading" text="Cargando documentos…" />}
            {documents.error && <QueryError error={documents.error} onRetry={() => documents.refetch()} text="No se pudieron cargar los documentos" />}
            {documents.data && (
              <div className="table-scroll">
                <table>
                  <caption className="visually-hidden">Documentos del expediente seleccionado</caption>
                  <thead>
                    <tr><th scope="col">Documento</th><th scope="col">Número</th><th scope="col">Categoría</th><th scope="col">Versión actual</th><th scope="col">Creación</th></tr>
                  </thead>
                  <tbody>
                    {documents.data.map((document) => (
                      <tr key={document.documentId}>
                        <td className="wrap"><Link to={`/documents/${document.documentId}`}>{document.title}</Link></td>
                        <td className="mono">{document.documentNumber ?? "—"}</td>
                        <td>{document.category}</td>
                        <td>{document.currentVersion > 0
                          ? <span className="status-tag open"><i aria-hidden="true" />v{document.currentVersion}</span>
                          : <span className="muted-text">Sin archivo</span>}</td>
                        <td>{formatDate(document.createdAtUtc)}</td>
                      </tr>
                    ))}
                    {documents.data.length === 0 && <tr><td colSpan={5} className="empty">Este expediente todavía no tiene documentos.</td></tr>}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}

      {creating && record && (
        <CreateDocumentModal recordId={record.recordId} recordLabel={`${record.recordNumber} · ${record.title}`} onClose={() => setCreating(false)} />
      )}
    </>
  );
}
