import { useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ApiError, errorMessage } from "../../api/client";
import { documentsApi } from "../../api/endpoints";
import { useAuth } from "../../auth/AuthContext";
import { can } from "../../auth/permissions";
import { Icon } from "../../components/Icon";
import { ConfirmDialog } from "../../components/Modal";
import { PageState, QueryError } from "../../components/PageState";
import { useDownload } from "../../hooks/useDownload";
import { PageHeader } from "../../layout/Shell";
import { formatBytes, formatDateTime } from "../../utils/format";
import { ACCEPTED_EXTENSIONS, UPLOAD_HINT, useInvalidateDocuments } from "./documentQueries";

export function DocumentDetailPage() {
  const documentId = Number(useParams().documentId);
  const { user } = useAuth();
  const navigate = useNavigate();
  const invalidate = useInvalidateDocuments();
  const { download, busyKey, error: downloadError } = useDownload();
  const fileInput = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const document = useQuery({
    queryKey: ["documents", "detail", documentId],
    queryFn: () => documentsApi.get(documentId),
    enabled: Number.isInteger(documentId) && documentId > 0,
  });

  const upload = useMutation({
    mutationFn: (selected: File) => documentsApi.uploadVersion(documentId, selected),
    onSuccess: async () => {
      setFile(null);
      if (fileInput.current) fileInput.current.value = "";
      await invalidate();
    },
  });

  const remove = useMutation({
    mutationFn: () => documentsApi.remove(documentId),
    onSuccess: async () => {
      const recordId = document.data?.recordId;
      await invalidate();
      navigate(recordId ? `/documents?recordId=${recordId}` : "/documents", { replace: true });
    },
  });

  if (document.isLoading) return <PageState kind="loading" text="Cargando documento…" />;
  if (document.error instanceof ApiError && document.error.status === 404) {
    return <PageState kind="empty" title="Documento no disponible" text="El documento no existe o fue eliminado." action={<Link className="see-all" to="/documents">Volver a documentos</Link>} />;
  }
  if (document.error || !document.data) return <QueryError error={document.error} onRetry={() => document.refetch()} />;

  const data = document.data;
  const manage = can.manageRecords(user) && !data.recordIsFinal;

  const submitUpload = (event: FormEvent) => {
    event.preventDefault();
    if (file) upload.mutate(file);
  };

  return (
    <>
      <Link className="back-link" to={`/documents?recordId=${data.recordId}`}><Icon name="arrowLeft" size={16} /> Documentos del expediente</Link>
      <PageHeader
        eyebrow={`${data.category}${data.documentNumber ? ` · ${data.documentNumber}` : ""}`}
        title={data.title}
        description={<>Expediente <Link to={`/records/${data.recordId}`} className="mono">{data.recordNumber}</Link> · {data.recordTitle}</>}
        actions={
          <>
            {data.currentVersion > 0 && (
              <button type="button" className="primary compact" disabled={busyKey !== null} onClick={() => download(documentId)}>
                <Icon name="download" size={18} /> {busyKey === `${documentId}:current` ? "Descargando…" : "Descargar actual"}
              </button>
            )}
            {manage && (
              <button type="button" className="danger compact" onClick={() => setConfirmDelete(true)}>
                <Icon name="trash" size={18} /> Eliminar
              </button>
            )}
          </>
        }
      />

      {downloadError && <p className="form-error" role="alert">{downloadError}</p>}
      {data.recordIsFinal && <p className="notice" role="status">El expediente está cerrado: el documento es de solo lectura.</p>}

      <section className="detail-grid">
        <dl className="detail-card">
          <div><dt>Versión actual</dt><dd>{data.currentVersion > 0 ? `v${data.currentVersion} · ${data.currentFileName}` : "Sin archivo cargado"}</dd></div>
          <div><dt>Creado por</dt><dd>{data.createdByName}</dd></div>
          <div><dt>Creación</dt><dd>{formatDateTime(data.createdAtUtc)}</dd></div>
          <div><dt>Última modificación</dt><dd>{formatDateTime(data.updatedAtUtc)}</dd></div>
          <div className="span-all"><dt>Descripción</dt><dd className="wrap">{data.description || "Sin descripción."}</dd></div>
        </dl>

        {manage && (
          <form className="upload-card" onSubmit={submitUpload} aria-labelledby="upload-title">
            <h3 id="upload-title">{data.currentVersion > 0 ? "Subir nueva versión" : "Subir primera versión"}</h3>
            <p className="field-hint">{UPLOAD_HINT} La versión anterior se conserva en el historial.</p>
            <label className="file-drop">
              <Icon name="upload" />
              <span>{file ? `${file.name} · ${formatBytes(file.size)}` : "Seleccionar archivo"}</span>
              <input ref={fileInput} type="file" accept={ACCEPTED_EXTENSIONS} onChange={(event) => { upload.reset(); setFile(event.target.files?.[0] ?? null); }} />
            </label>
            {upload.error && <p className="form-error" role="alert">{errorMessage(upload.error)}</p>}
            {upload.isSuccess && <p className="form-success" role="status">Versión registrada correctamente.</p>}
            <button type="submit" className="primary compact" disabled={!file || upload.isPending}>
              {upload.isPending ? "Subiendo…" : "Subir versión"}
            </button>
          </form>
        )}
      </section>

      <section className="records-panel section-gap" aria-labelledby="versions-title">
        <div className="records-panel-head">
          <div>
            <h3 id="versions-title">Historial de versiones</h3>
            <span>Cada versión conserva su hash SHA-256</span>
          </div>
        </div>
        <div className="table-card dashboard-table">
          <div className="table-scroll">
            <table>
              <caption className="visually-hidden">Versiones del documento</caption>
              <thead><tr><th scope="col">Versión</th><th scope="col">Archivo</th><th scope="col">Tamaño</th><th scope="col">Subido por</th><th scope="col">Fecha</th><th scope="col">SHA-256</th><th scope="col"><span className="visually-hidden">Acciones</span></th></tr></thead>
              <tbody>
                {data.versions.map((version) => (
                  <tr key={version.documentVersionId}>
                    <td>
                      v{version.versionNumber}
                      {version.isCurrent && <span className="status-tag open inline-tag"><i aria-hidden="true" />Actual</span>}
                    </td>
                    <td className="wrap">{version.originalFileName}</td>
                    <td>{formatBytes(version.fileSizeBytes)}</td>
                    <td>{version.uploadedByName}</td>
                    <td>{formatDateTime(version.uploadedAtUtc)}</td>
                    <td className="mono hash" title={version.sha256Hash}>{version.sha256Hash.slice(0, 12)}…</td>
                    <td>
                      <button
                        type="button"
                        className="secondary compact"
                        disabled={busyKey !== null}
                        aria-label={`Descargar versión ${version.versionNumber}`}
                        onClick={() => download(documentId, version.versionNumber)}
                      >
                        <Icon name="download" size={16} /> {busyKey === `${documentId}:${version.versionNumber}` ? "…" : "Descargar"}
                      </button>
                    </td>
                  </tr>
                ))}
                {data.versions.length === 0 && <tr><td colSpan={7} className="empty">Aún no se ha cargado ninguna versión.</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      </section>

      {confirmDelete && (
        <ConfirmDialog
          title="Eliminar documento"
          message={`"${data.title}" dejará de mostrarse en el expediente. La eliminación es lógica: el registro y sus versiones se conservan para auditoría.`}
          confirmLabel="Eliminar documento"
          busy={remove.isPending}
          error={remove.error ? errorMessage(remove.error) : null}
          onConfirm={() => remove.mutate()}
          onClose={() => setConfirmDelete(false)}
        />
      )}
    </>
  );
}
