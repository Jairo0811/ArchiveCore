import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { sqlLabApi } from "../../api/endpoints";
import { Icon } from "../../components/Icon";
import { PageState, QueryError } from "../../components/PageState";

function renderCell(value: unknown): string {
  if (value === null || value === undefined) return "NULL";
  if (typeof value === "boolean") return value ? "Sí" : "No";
  return String(value);
}

/**
 * SOF-008 laboratory: Administrator only, predefined read-only queries. There is no SQL editor;
 * the button only asks the API to run one of its server-side definitions.
 */
export function SqlLabPage() {
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [requested, setRequested] = useState<string | null>(null);

  const definitions = useQuery({ queryKey: ["sql-lab", "definitions"], queryFn: sqlLabApi.queries, staleTime: Infinity });
  const execution = useQuery({
    queryKey: ["sql-lab", "execute", requested],
    queryFn: () => sqlLabApi.execute(requested!),
    enabled: requested !== null,
    staleTime: 0,
  });

  const list = definitions.data ?? [];
  const selected = list.find((item) => item.key === selectedKey) ?? list[0];
  const showingResult = execution.data && execution.data.query.key === selected?.key;

  const run = () => {
    if (!selected) return;
    if (requested === selected.key) void execution.refetch();
    else setRequested(selected.key);
  };

  return (
    <div className="sql-lab-page">
      <header className="page-header sql-lab-header">
        <div>
          <p className="eyebrow subtle">Bases de Datos Avanzadas · SOF-008</p>
          <h2>Laboratorio SQL</h2>
          <p className="sql-lab-intro">
            Consulta el SQL original del proyecto y ejecuta su equivalente moderno
            sobre ArchiveCoreDb. Las consultas disponibles son predefinidas y de solo lectura:
            no existe consola para escribir SQL.
          </p>
        </div>
        <span className="status-pill"><span className="status-dot" aria-hidden="true" />Solo lectura · consultas predefinidas</span>
      </header>

      {definitions.isLoading && <PageState kind="loading" text="Cargando consultas académicas…" />}
      {definitions.error && <QueryError error={definitions.error} onRetry={() => definitions.refetch()} text="No se pudo cargar el laboratorio SQL" />}

      {definitions.data && selected && (
        <>
          <section className="sql-query-selector" role="tablist" aria-label="Consultas disponibles">
            {list.map((query) => (
              <button
                key={query.key}
                type="button"
                role="tab"
                aria-selected={query.key === selected.key}
                className={query.key === selected.key ? "active" : ""}
                onClick={() => setSelectedKey(query.key)}
              >
                <Icon name="database" />
                <span>{query.title}</span>
              </button>
            ))}
          </section>

          <section className="sql-lab-grid" role="tabpanel" aria-label={selected.title}>
            <article className="sql-query-card legacy">
              <div className="sql-card-head">
                <div>
                  <span className="sql-badge">LEGACY</span>
                  <h3>Consulta original</h3>
                </div>
                <span>DataBaseProject</span>
              </div>
              <pre className="sql-code" tabIndex={0} aria-label="SQL original"><code>{selected.legacySql}</code></pre>
              <p>
                Consulta preservada literalmente del proyecto académico original
                desarrollado para Bases de Datos Avanzadas (2017-C1).
              </p>
              <p className="sql-source-path">
                Fuente legacy (copia pública revisada): <code>{selected.sourcePath}</code>
              </p>
              <p className="sql-privacy-note">
                Los INSERT históricos y sus datos de ejemplo no se exponen en esta interfaz.
              </p>
            </article>

            <article className="sql-query-card modern">
              <div className="sql-card-head">
                <div>
                  <span className="sql-badge modern">ARCHIVECORE</span>
                  <h3>Equivalente moderno</h3>
                </div>
                <span>ArchiveCoreDb</span>
              </div>
              <pre className="sql-code" tabIndex={0} aria-label="SQL moderno"><code>{selected.modernSql}</code></pre>
              <p>{selected.description}</p>
              <button type="button" className="primary compact" disabled={execution.isFetching} onClick={run}>
                {execution.isFetching ? "Ejecutando…" : "Ejecutar consulta"}
              </button>
            </article>
          </section>

          <section className="sql-result-card" aria-labelledby="sql-result-title" aria-busy={execution.isFetching}>
            <div className="records-panel-head">
              <div>
                <h3 id="sql-result-title">Resultado</h3>
                <span>Datos reales leídos desde SQL Server</span>
              </div>
              {showingResult && (
                <span className="sql-row-count" role="status">{execution.data!.rows.length} fila(s)</span>
              )}
            </div>

            {execution.isFetching && <PageState kind="loading" text="Ejecutando consulta…" />}
            {!execution.isFetching && execution.error && <QueryError error={execution.error} text="No se pudo ejecutar la consulta académica" />}
            {!execution.isFetching && !execution.error && !showingResult && (
              <PageState kind="info" text="Selecciona una consulta y presiona «Ejecutar consulta»." />
            )}

            {!execution.isFetching && showingResult && (
              <div className="sql-result-table table-scroll">
                <table>
                  <caption className="visually-hidden">Resultado de {selected.title}</caption>
                  <thead>
                    <tr>{execution.data!.columns.map((column) => <th scope="col" key={column}>{column}</th>)}</tr>
                  </thead>
                  <tbody>
                    {execution.data!.rows.map((row, index) => (
                      <tr key={index}>
                        {execution.data!.columns.map((column) => <td key={column}>{renderCell(row[column])}</td>)}
                      </tr>
                    ))}
                    {execution.data!.rows.length === 0 && (
                      <tr><td colSpan={execution.data!.columns.length || 1} className="empty">La consulta no devolvió filas.</td></tr>
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}
