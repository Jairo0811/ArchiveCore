import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { ApiError } from "../../api/client";
import { documentsApi, recordsApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { can } from "../../auth/permissions";
import { Icon } from "../../components/Icon";
import { PageState, QueryError } from "../../components/PageState";
import { StatusTag } from "../../components/StatusTag";
import { PageHeader } from "../../layout/Shell";
import { formatDate, formatDateTime } from "../../utils/format";
import { CreateDocumentModal } from "../Documents/CreateDocumentModal";
import { EditRecordModal } from "./RecordFormModal";
import { recordKeys, useRecord } from "./recordQueries";
import { CloseRecordDialog, TransferModal } from "./WorkflowModals";

type Dialog = "edit" | "transfer" | "close" | "document" | null;

export function RecordDetailPage() {
  const recordId = Number(useParams().recordId);
  const { user } = useAuth();
  const [dialog, setDialog] = useState<Dialog>(null);

  const record = useRecord(recordId);
  const history = useQuery({ queryKey: recordKeys.history(recordId), queryFn: () => recordsApi.history(recordId), enabled: record.isSuccess });
  const documents = useQuery({ queryKey: ["documents", "record", recordId], queryFn: () => documentsApi.listByRecord(recordId), enabled: record.isSuccess });

  if (!Number.isInteger(recordId) || recordId <= 0) return <PageState kind="empty" title="Expediente no válido" text="El identificador del expediente no es válido." />;
  if (record.isLoading) return <PageState kind="loading" text="Cargando expediente…" />;
  if (record.error instanceof ApiError && record.error.status === 404) {
    return <PageState kind="empty" title="Expediente no encontrado" text="El expediente no existe." action={<Link className="see-all" to="/records">Volver a expedientes</Link>} />;
  }
  if (record.error || !record.data) return <QueryError error={record.error} onRetry={() => record.refetch()} />;

  const data = record.data;
  const manage = can.manageRecords(user) && !data.isFinal;

  return (
    <>
      <Link className="back-link" to="/records"><Icon name="arrowLeft" size={16} /> Expedientes</Link>
      <PageHeader
        eyebrow={data.recordNumber}
        title={data.title}
        description={<StatusTag code={data.statusCode} label={data.status} />}
        actions={manage && (
          <>
            <button type="button" className="secondary compact" onClick={() => setDialog("edit")}><Icon name="edit" size={18} /> Editar</button>
            <button type="button" className="secondary compact" onClick={() => setDialog("transfer")}>
              <Icon name="swap" size={18} /> {data.assignedToUserId ? "Transferir" : "Asignar"}
            </button>
            <button type="button" className="danger compact" onClick={() => setDialog("close")}><Icon name="archive" size={18} /> Cerrar</button>
          </>
        )}
      />

      {data.isFinal && (
        <p className="notice" role="status">Este expediente está {data.status.toLowerCase()} y es de solo lectura.</p>
      )}

      <section className="detail-grid" aria-label="Datos del expediente">
        <dl className="detail-card">
          <div><dt>Código</dt><dd className="mono">{data.recordNumber}</dd></div>
          <div><dt>Responsable</dt><dd>{data.assignedToName ?? "Sin asignar"}</dd></div>
          <div><dt>Creado por</dt><dd>{data.createdByName}</dd></div>
          <div><dt>Apertura</dt><dd>{formatDateTime(data.openedAtUtc)}</dd></div>
          <div><dt>Última modificación</dt><dd>{formatDateTime(data.updatedAtUtc)}</dd></div>
          <div><dt>Cierre</dt><dd>{formatDateTime(data.closedAtUtc)}</dd></div>
          <div className="span-all"><dt>Descripción</dt><dd className="wrap">{data.description || "Sin descripción."}</dd></div>
        </dl>
      </section>

      <section className="records-panel section-gap" aria-labelledby="record-documents-title">
        <div className="records-panel-head">
          <div>
            <h3 id="record-documents-title">Documentos</h3>
            <span>Documentos activos del expediente</span>
          </div>
          {manage && (
            <button type="button" className="primary compact" onClick={() => setDialog("document")}>
              <Icon name="plus" size={18} /> Nuevo documento
            </button>
          )}
        </div>
        <div className="table-card dashboard-table">
          {documents.isLoading && <PageState kind="loading" text="Cargando documentos…" />}
          {documents.error && <QueryError error={documents.error} onRetry={() => documents.refetch()} />}
          {documents.data && (
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Documentos del expediente</caption>
                <thead><tr><th scope="col">Documento</th><th scope="col">Número</th><th scope="col">Categoría</th><th scope="col">Versión actual</th><th scope="col">Actualizado</th></tr></thead>
                <tbody>
                  {documents.data.map((document) => (
                    <tr key={document.documentId}>
                      <td className="wrap"><Link to={`/documents/${document.documentId}`}>{document.title}</Link></td>
                      <td className="mono">{document.documentNumber ?? "—"}</td>
                      <td>{document.category}</td>
                      <td>{document.currentVersion > 0 ? `v${document.currentVersion} · ${document.currentFileName}` : <span className="muted-text">Sin archivo</span>}</td>
                      <td>{formatDate(document.updatedAtUtc ?? document.createdAtUtc)}</td>
                    </tr>
                  ))}
                  {documents.data.length === 0 && <tr><td colSpan={5} className="empty">Este expediente todavía no tiene documentos.</td></tr>}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>

      <section className="records-panel section-gap" aria-labelledby="record-history-title">
        <div className="records-panel-head">
          <div>
            <h3 id="record-history-title">Historial de movimientos</h3>
            <span>Asignaciones, transferencias y cierre</span>
          </div>
        </div>
        <div className="table-card dashboard-table">
          {history.isLoading && <PageState kind="loading" text="Cargando historial…" />}
          {history.error && <QueryError error={history.error} onRetry={() => history.refetch()} />}
          {history.data && (
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Historial de movimientos</caption>
                <thead><tr><th scope="col">Fecha</th><th scope="col">Movimiento</th><th scope="col">Desde</th><th scope="col">Hacia</th><th scope="col">Realizado por</th><th scope="col">Notas</th></tr></thead>
                <tbody>
                  {history.data.map((movement) => (
                    <tr key={movement.recordMovementId}>
                      <td>{formatDateTime(movement.performedAtUtc)}</td>
                      <td>{movement.movementType}</td>
                      <td>{movement.fromUserName ?? "—"}</td>
                      <td>{movement.toUserName ?? "—"}</td>
                      <td>{movement.performedByName}</td>
                      <td className="wrap">{movement.notes ?? "—"}</td>
                    </tr>
                  ))}
                  {history.data.length === 0 && <tr><td colSpan={6} className="empty">Sin movimientos registrados.</td></tr>}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>

      {dialog === "edit" && <EditRecordModal record={data} onClose={() => setDialog(null)} />}
      {dialog === "transfer" && <TransferModal record={data} onClose={() => setDialog(null)} />}
      {dialog === "close" && <CloseRecordDialog record={data} onClose={() => setDialog(null)} />}
      {dialog === "document" && <CreateDocumentModal recordId={data.recordId} recordLabel={`${data.recordNumber} · ${data.title}`} onClose={() => setDialog(null)} />}
    </>
  );
}
