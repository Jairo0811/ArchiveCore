import { useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { dashboardApi, recordsApi } from "../../api/endpoints";
import { Icon } from "../../components/Icon";
import { PageState, QueryError } from "../../components/PageState";
import { StatusTag } from "../../components/StatusTag";
import { formatDate, formatDateTime, formatNumber } from "../../utils/format";

export function DashboardPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");

  const summary = useQuery({ queryKey: ["dashboard"], queryFn: dashboardApi.summary });
  const health = useQuery({ queryKey: ["health"], queryFn: dashboardApi.health, refetchInterval: 60_000 });
  const recent = useQuery({
    queryKey: ["records", "list", { page: 1, pageSize: 5 }],
    queryFn: () => recordsApi.list({ page: 1, pageSize: 5 }),
  });

  if (summary.isLoading) return <PageState kind="loading" text="Cargando dashboard…" />;
  if (summary.error || !summary.data) return <QueryError error={summary.error} onRetry={() => summary.refetch()} text="No se pudo cargar el dashboard" />;

  const data = summary.data;
  const cards = [
    { label: "Expedientes activos", value: data.activeRecords, icon: "folder", tone: "cyan" },
    { label: "Documentos activos", value: data.activeDocuments, icon: "file", tone: "blue" },
    { label: "Usuarios activos", value: data.activeUsers, icon: "users", tone: "blue" },
    { label: "Movimientos hoy (UTC)", value: data.movementsToday, icon: "swap", tone: "teal" },
    { label: "Expedientes cerrados", value: data.closedRecords, icon: "archive", tone: "muted" },
    { label: "Eventos de auditoría hoy (UTC)", value: data.auditEventsToday, icon: "shield", tone: "cyan" },
  ];

  const healthPill =
    health.isLoading ? { tone: "pending", text: "Comprobando servicio…" }
      : health.data ? { tone: "ok", text: "API y base de datos disponibles" }
        : { tone: "down", text: "Servicio no verificado" };

  const submitSearch = (event: FormEvent) => {
    event.preventDefault();
    navigate(search.trim() ? `/records?search=${encodeURIComponent(search.trim())}` : "/records");
  };

  const records = recent.data?.items ?? [];

  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="eyebrow subtle">Resumen</p>
          <div className="dashboard-title-row">
            <h2>Centro de control</h2>
            <span className={`status-pill ${healthPill.tone}`} role="status">
              <span className="status-dot" aria-hidden="true" />{healthPill.text}
            </span>
          </div>
        </div>
        <div className="header-message">
          <span>Gestión segura de la información</span>
          <span>para un mejor mañana.</span>
          <i />
        </div>
      </header>

      <section className="metric-grid" aria-label="Indicadores actuales">
        {cards.map((card) => (
          <article className={"metric-card " + card.tone} key={card.label}>
            <div className="metric-icon"><Icon name={card.icon} size={28} /></div>
            <div className="metric-copy">
              <span>{card.label}</span>
              <div className="metric-value-row">
                <strong>{formatNumber(card.value)}</strong>
              </div>
            </div>
          </article>
        ))}
      </section>
      <p className="metric-footnote">Valores actuales calculados por la API · actualizado {formatDateTime(data.generatedAtUtc)}</p>

      <section className="traceability-panel">
        <div className="trace-copy">
          <p className="eyebrow">ArchiveCore</p>
          <h3>Trazabilidad desde la base de datos</h3>
          <p>
            Los cambios críticos quedan respaldados mediante auditoría de SQL Server,
            versionado de documentos y seguimiento de movimientos de expedientes,
            garantizando integridad, transparencia y control en todo el ciclo de vida de la información.
          </p>

          <div className="trace-features">
            <span><Icon name="database" /> Auditoría en SQL Server</span>
            <span><Icon name="file" /> Versionado de documentos</span>
            <span><Icon name="network" /> Seguimiento de movimientos</span>
          </div>
        </div>

        <div className="trace-visual" aria-hidden="true">
          <div className="server-lines" />
          <div className="security-stack">
            <span><Icon name="database" /> AUDIT</span>
            <span><Icon name="file" /> VERSION</span>
            <span><Icon name="network" /> TRACK</span>
            <span><Icon name="lock" /> SECURE</span>
          </div>
          <div className="trust-copy">PERSONAS<br />PROCESOS<br />INFORMACIÓN<br />CONFIANZA<i /></div>
        </div>
      </section>

      <section className="records-panel" aria-labelledby="recent-records-title">
        <div className="records-panel-head">
          <div>
            <h3 id="recent-records-title">Expedientes recientes</h3>
            <span>Últimos expedientes abiertos</span>
          </div>
          <Link className="see-all" to="/records">Ver todos →</Link>
        </div>

        <form className="records-toolbar" role="search" onSubmit={submitSearch}>
          <label className="search-shell">
            <Icon name="search" />
            <span className="visually-hidden">Buscar expedientes</span>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar código o título…" />
          </label>
          <button type="submit" className="secondary compact">Buscar</button>
        </form>

        <div className="table-card dashboard-table">
          {recent.isLoading && <PageState kind="loading" text="Cargando expedientes…" />}
          {recent.error && <QueryError error={recent.error} onRetry={() => recent.refetch()} />}
          {recent.data && (
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Expedientes recientes</caption>
                <thead>
                  <tr><th scope="col">Código</th><th scope="col">Título</th><th scope="col">Estado</th><th scope="col">Responsable</th><th scope="col">Apertura</th></tr>
                </thead>
                <tbody>
                  {records.map((record) => (
                    <tr key={record.recordId}>
                      <td className="mono"><Link to={`/records/${record.recordId}`}>{record.recordNumber}</Link></td>
                      <td>{record.title}</td>
                      <td><StatusTag code={record.statusCode} label={record.status} /></td>
                      <td>{record.assignedToName ?? "Sin asignar"}</td>
                      <td>{formatDate(record.openedAtUtc)}</td>
                    </tr>
                  ))}
                  {records.length === 0 && (
                    <tr><td colSpan={5} className="empty">Todavía no hay expedientes registrados.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
