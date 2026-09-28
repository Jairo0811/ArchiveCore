import { FormEvent, ReactNode, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Navigate, NavLink, Route, Routes, useNavigate } from "react-router-dom";
import { api, AuthResponse, login } from "./api";

type DashboardSummary = {
  activeUsers: number;
  activeRecords: number;
  closedRecords: number;
  activeDocuments: number;
  movementsToday: number;
  auditEventsToday: number;
};

type RecordSummary = {
  recordId: number;
  recordNumber: string;
  title: string;
  status: string;
  assignedToUserId: number | null;
  openedAtUtc: string;
  closedAtUtc: string | null;
};

type DocumentSummary = {
  documentId: number;
  recordId: number;
  title: string;
  documentNumber: string | null;
  category: string;
  currentVersion: number;
  createdAtUtc: string;
};

type AuditEventSummary = {
  auditEventId: number;
  userId: number | null;
  entityName: string;
  entityKey: string;
  actionType: string;
  createdAtUtc: string;
};

type SqlLabQueryDefinition = {
  key: string;
  title: string;
  legacySql: string;
  modernSql: string;
  description: string;
};

type SqlLabQueryResult = {
  query: SqlLabQueryDefinition;
  columns: string[];
  rows: Record<string, unknown>[];
};

const STORAGE_KEY = "archivecore.auth";

function readAuth(): AuthResponse | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try { return JSON.parse(raw) as AuthResponse; }
  catch { return null; }
}

export default function App() {
  const [auth, setAuth] = useState<AuthResponse | null>(() => readAuth());

  const signIn = (value: AuthResponse) => {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    setAuth(value);
  };

  const signOut = () => {
    sessionStorage.removeItem(STORAGE_KEY);
    setAuth(null);
  };

  return (
    <Routes>
      <Route path="/login" element={auth ? <Navigate to="/" replace /> : <LoginPage onLogin={signIn} />} />
      <Route path="/*" element={auth ? <Shell auth={auth} onLogout={signOut} /> : <Navigate to="/login" replace />} />
    </Routes>
  );
}

function Icon({ name, size = 20 }: { name: string; size?: number }) {
  const paths: Record<string, ReactNode> = {
    home: <><path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10.5V20h13v-9.5"/><path d="M9 20v-6h6v6"/></>,
    folder: <><path d="M3 6h6l2 2h10v10H3z"/></>,
    file: <><path d="M6 3h8l4 4v14H6z"/><path d="M14 3v5h5"/><path d="M9 13h6M9 17h6"/></>,
    chart: <><path d="M4 20V10h4v10M10 20V5h4v15M16 20V8h4v12"/></>,
    users: <><circle cx="9" cy="8" r="3"/><circle cx="17" cy="9" r="2.5"/><path d="M3.5 20c.5-4 2.7-6 5.5-6s5 2 5.5 6"/><path d="M14 14.5c2.5.2 4.3 2 4.8 5.5"/></>,
    swap: <><path d="M4 8h13l-3-3M20 16H7l3 3"/><path d="M17 5l3 3-3 3M7 13l-3 3 3 3"/></>,
    archive: <><rect x="4" y="7" width="16" height="13" rx="2"/><path d="M3 4h18v4H3z"/><path d="M9 12h6"/></>,
    shield: <><path d="M12 3l8 3v5c0 5-3 8-8 10-5-2-8-5-8-10V6z"/><path d="m9 12 2 2 4-5"/></>,
    database: <><ellipse cx="12" cy="5" rx="7" ry="3"/><path d="M5 5v6c0 1.7 3.1 3 7 3s7-1.3 7-3V5"/><path d="M5 11v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6"/></>,
    network: <><circle cx="12" cy="5" r="2"/><circle cx="5" cy="18" r="2"/><circle cx="19" cy="18" r="2"/><path d="M12 7v4M12 11 6 16M12 11l6 5"/></>,
    lock: <><rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/></>,
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
    logout: <><path d="M10 4H5v16h5"/><path d="M14 8l4 4-4 4M18 12H9"/></>,
  };
  return (
    <svg className="icon" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {paths[name] ?? paths.file}
    </svg>
  );
}

function Sparkline({ variant = 0 }: { variant?: number }) {
  const d = [
    "M2 23 C14 10, 20 27, 32 15 S50 23, 62 9",
    "M2 22 C10 19, 17 8, 27 14 S42 24, 62 7",
    "M2 19 C13 26, 18 7, 30 12 S47 24, 62 10",
  ][variant % 3];

  return (
    <svg className="sparkline" viewBox="0 0 64 30" aria-hidden="true">
      <defs>
        <linearGradient id={"spark-" + variant} x1="0" y1="0" x2="1" y2="0">
          <stop offset="0" stopColor="#1bb7c7" />
          <stop offset="1" stopColor="#4fe7dd" />
        </linearGradient>
      </defs>
      <path d={d} fill="none" stroke={"url(#spark-" + variant + ")"} strokeWidth="2.5" />
    </svg>
  );
}

function LoginPage({ onLogin }: { onLogin: (value: AuthResponse) => void }) {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");

    try {
      const result = await login(email, password);
      onLogin(result);
      navigate("/");
    } catch (error) {
      const message = error instanceof Error ? error.message : "";

      if (message.includes("Failed to fetch") || message.includes("NetworkError")) {
        setError("No se pudo conectar con la API de ArchiveCore.");
      } else {
        setError("No pudimos iniciar sesión. Verifica tus credenciales.");
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-card">
        <img className="login-logo" src="/archivecore-mark.svg" alt="ArchiveCore" />
        <p className="eyebrow">Document & Records Management</p>
        <h1>Archive<span>Core</span></h1>
        <p className="muted">Información segura, trazable y bajo control.</p>

        <form onSubmit={submit}>
          <label>
            Correo
            <input type="email" autoComplete="username" value={email}
              onChange={(event) => setEmail(event.target.value)} required />
          </label>
          <label>
            Contraseña
            <input type="password" autoComplete="current-password" value={password}
              onChange={(event) => setPassword(event.target.value)} required />
          </label>
          {error && <p className="error">{error}</p>}
          <button className="primary" disabled={busy}>{busy ? "Entrando…" : "Iniciar sesión"}</button>
        </form>
      </section>
    </main>
  );
}

function Shell({ auth, onLogout }: { auth: AuthResponse; onLogout: () => void }) {
  const initials = useMemo(
    () => `${auth.user.firstName[0] ?? ""}${auth.user.lastName[0] ?? ""}`.toUpperCase(),
    [auth]
  );

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <img src="/archivecore-mark.svg" alt="" />
          <div>
            <strong>ArchiveCore</strong>
            <small>Control documental</small>
          </div>
        </div>

        <nav>
          <NavLink to="/" end><Icon name="home" /> <span>Dashboard</span></NavLink>
          <NavLink to="/records"><Icon name="folder" /> <span>Expedientes</span></NavLink>
          <NavLink to="/documents"><Icon name="file" /> <span>Documentos</span></NavLink>
          {auth.user.roles.includes("Administrator") && (
            <>
              <NavLink to="/audit"><Icon name="chart" /> <span>Auditoría</span></NavLink>
              <NavLink to="/sql-lab"><Icon name="database" /> <span>SQL Lab SOF-008</span></NavLink>
            </>
          )}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="avatar">{initials}</div>
            <div className="sidebar-user-copy">
              <strong>{auth.user.firstName} {auth.user.lastName}</strong>
              <small>{auth.user.roles.join(" · ")}</small>
            </div>
          </div>

          <button className="logout-button" onClick={onLogout}>
            <Icon name="logout" /> <span>Salir</span>
          </button>

          <p className="sidebar-slogan">DOCUMENTOS HOY,<br/>INSTITUCIONES MAÑANA</p>
        </div>
      </aside>

      <main className="content">
        <Routes>
          <Route path="/" element={<Dashboard token={auth.accessToken} />} />
          <Route path="/records" element={<Records token={auth.accessToken} />} />
          <Route path="/documents" element={<Documents token={auth.accessToken} />} />
          <Route path="/audit" element={<Audit token={auth.accessToken} />} />
          <Route
            path="/sql-lab"
            element={
              auth.user.roles.includes("Administrator")
                ? <SqlLab token={auth.accessToken} />
                : <Navigate to="/" replace />
            }
          />
        </Routes>
      </main>
    </div>
  );
}

function Dashboard({ token }: { token: string }) {
  const dashboardQuery = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => api<DashboardSummary>("/dashboard", {}, token),
  });

  const recordsQuery = useQuery({
    queryKey: ["records", "dashboard-preview"],
    queryFn: () => api<RecordSummary[]>("/records", {}, token),
  });

  if (dashboardQuery.isLoading) return <PageState text="Cargando dashboard…" />;
  if (dashboardQuery.error || !dashboardQuery.data) return <PageState text="No se pudo cargar el dashboard." />;

  const data = dashboardQuery.data;
  const records = (recordsQuery.data ?? []).slice(0, 5);

  const cards = [
    { label: "Expedientes activos", value: data.activeRecords, icon: "folder", trend: "+12%", tone: "cyan" },
    { label: "Documentos activos", value: data.activeDocuments, icon: "file", trend: "+8%", tone: "blue" },
    { label: "Usuarios activos", value: data.activeUsers, icon: "users", trend: "+9%", tone: "blue" },
    { label: "Movimientos hoy", value: data.movementsToday, icon: "swap", trend: "+19%", tone: "teal" },
    { label: "Expedientes cerrados", value: data.closedRecords, icon: "archive", trend: "—", tone: "muted" },
    { label: "Eventos de auditoría hoy", value: data.auditEventsToday, icon: "shield", trend: "+27%", tone: "cyan" },
  ];

  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="eyebrow subtle">Overview</p>
          <div className="dashboard-title-row">
            <h2>Centro de control</h2>
            <span className="status-pill"><span className="status-dot" />Sistema operativo</span>
          </div>
        </div>
        <div className="header-message">
          <span>Gestión segura de la información</span>
          <span>para un mejor mañana.</span>
          <i />
        </div>
      </header>

      <section className="metric-grid">
        {cards.map((card, index) => (
          <article className={"metric-card " + card.tone} key={card.label}>
            <div className="metric-icon"><Icon name={card.icon} size={28} /></div>
            <div className="metric-copy">
              <span>{card.label}</span>
              <div className="metric-value-row">
                <strong>{card.value}</strong>
                <small>{card.trend}</small>
              </div>
            </div>
            <Sparkline variant={index} />
          </article>
        ))}
      </section>

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
          <div className="trust-copy">PERSONAS<br/>PROCESOS<br/>INFORMACIÓN<br/>CONFIANZA<i /></div>
        </div>
      </section>

      <section className="records-panel">
        <div className="records-panel-head">
          <div>
            <h3>Expedientes recientes</h3>
            <span>Últimos registros disponibles</span>
          </div>
          <NavLink className="see-all" to="/records">Ver todos →</NavLink>
        </div>

        <div className="records-toolbar">
          <div className="search-shell"><Icon name="search" /><span>Buscar código o título…</span></div>
          <button className="filter-button" aria-label="Filtros">☷</button>
        </div>

        <div className="table-card dashboard-table">
          {recordsQuery.isLoading && <PageState text="Cargando expedientes…" />}
          {!recordsQuery.isLoading && (
            <table>
              <thead>
                <tr><th>Código</th><th>Título</th><th>Estado</th><th>Responsable</th><th>Apertura</th><th /></tr>
              </thead>
              <tbody>
                {records.map((record) => (
                  <tr key={record.recordId}>
                    <td className="mono">{record.recordNumber}</td>
                    <td>{record.title}</td>
                    <td><StatusTag value={record.status} /></td>
                    <td>{record.assignedToUserId ?? "Sin asignar"}</td>
                    <td>{new Date(record.openedAtUtc).toLocaleDateString()}</td>
                    <td className="row-menu">•••</td>
                  </tr>
                ))}
                {records.length === 0 && (
                  <tr><td colSpan={6} className="empty">No hay expedientes recientes.</td></tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </div>
  );
}

function StatusTag({ value }: { value: string }) {
  const normalized = value.toLowerCase();
  const state = normalized.includes("close") || normalized.includes("cerr") ? "closed"
    : normalized.includes("review") || normalized.includes("revis") ? "review"
    : "open";

  return <span className={"status-tag " + state}><i />{value}</span>;
}

function Records({ token }: { token: string }) {
  const [search, setSearch] = useState("");
  const { data = [], isLoading, error } = useQuery({
    queryKey: ["records", search],
    queryFn: () => api<RecordSummary[]>(
      `/records${search ? `?search=${encodeURIComponent(search)}` : ""}`,
      {},
      token
    ),
  });

  return (
    <>
      <header className="page-header">
        <div><p className="eyebrow subtle">Records</p><h2>Expedientes</h2></div>
        <div className="search-input-wrap"><Icon name="search" /><input className="search" placeholder="Buscar código o título…" value={search}
          onChange={(event) => setSearch(event.target.value)} /></div>
      </header>
      <section className="table-card">
        {isLoading && <PageState text="Cargando expedientes…" />}
        {error && <PageState text="No se pudieron cargar los expedientes." />}
        {!isLoading && !error && (
          <table>
            <thead><tr><th>Código</th><th>Título</th><th>Estado</th><th>Responsable</th><th>Apertura</th></tr></thead>
            <tbody>
              {data.map((record) => (
                <tr key={record.recordId}>
                  <td className="mono">{record.recordNumber}</td>
                  <td>{record.title}</td>
                  <td><StatusTag value={record.status} /></td>
                  <td>{record.assignedToUserId ?? "Sin asignar"}</td>
                  <td>{new Date(record.openedAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={5} className="empty">No hay expedientes para mostrar.</td></tr>}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}

function Documents({ token }: { token: string }) {
  const [recordId, setRecordId] = useState("1");
  const { data = [], isLoading, error, refetch } = useQuery({
    queryKey: ["documents", recordId],
    queryFn: () => api<DocumentSummary[]>("/documents/record/" + recordId, {}, token),
    enabled: false,
  });

  return (
    <>
      <header className="page-header">
        <div><p className="eyebrow subtle">Documents</p><h2>Documentos</h2></div>
        <div className="inline-tools">
          <input className="search" type="number" min="1" value={recordId}
            onChange={(event) => setRecordId(event.target.value)} placeholder="ID expediente" />
          <button className="primary compact" onClick={() => refetch()}>Consultar</button>
        </div>
      </header>
      <section className="table-card">
        {isLoading && <PageState text="Cargando documentos…" />}
        {error && <PageState text="No se pudieron cargar los documentos." />}
        {!isLoading && !error && (
          <table>
            <thead><tr><th>ID</th><th>Documento</th><th>Categoría</th><th>Versión</th><th>Creación</th></tr></thead>
            <tbody>
              {data.map((document) => (
                <tr key={document.documentId}>
                  <td className="mono">{document.documentNumber ?? document.documentId}</td>
                  <td>{document.title}</td>
                  <td>{document.category}</td>
                  <td><span className="status-tag open"><i />v{document.currentVersion}</span></td>
                  <td>{new Date(document.createdAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={5} className="empty">Consulta un expediente para ver sus documentos.</td></tr>}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}

function Audit({ token }: { token: string }) {
  const { data = [], isLoading, error } = useQuery({
    queryKey: ["audit"],
    queryFn: () => api<AuditEventSummary[]>("/audit", {}, token),
  });

  return (
    <>
      <header className="page-header">
        <div><p className="eyebrow subtle">Traceability</p><h2>Auditoría</h2></div>
      </header>
      <section className="table-card">
        {isLoading && <PageState text="Cargando auditoría…" />}
        {error && <PageState text="No se pudo cargar la auditoría o no tienes permisos." />}
        {!isLoading && !error && (
          <table>
            <thead><tr><th>Fecha</th><th>Entidad</th><th>Registro</th><th>Acción</th><th>Usuario</th></tr></thead>
            <tbody>
              {data.map((event) => (
                <tr key={event.auditEventId}>
                  <td>{new Date(event.createdAtUtc).toLocaleString()}</td>
                  <td>{event.entityName}</td>
                  <td className="mono">{event.entityKey}</td>
                  <td><span className="status-tag open"><i />{event.actionType}</span></td>
                  <td>{event.userId ?? "Sistema"}</td>
                </tr>
              ))}
              {data.length === 0 && <tr><td colSpan={5} className="empty">No hay eventos de auditoría.</td></tr>}
            </tbody>
          </table>
        )}
      </section>
    </>
  );
}

function SqlLab({ token }: { token: string }) {
  const [selectedKey, setSelectedKey] = useState("legacy-admins");

  const definitionsQuery = useQuery({
    queryKey: ["sql-lab", "definitions"],
    queryFn: () => api<SqlLabQueryDefinition[]>("/academic/sql-lab/queries", {}, token),
  });

  const executionQuery = useQuery({
    queryKey: ["sql-lab", "execute", selectedKey],
    queryFn: () => api<SqlLabQueryResult>(
      `/academic/sql-lab/queries/${encodeURIComponent(selectedKey)}`,
      {},
      token
    ),
    enabled: false,
  });

  const definitions = definitionsQuery.data ?? [];
  const selected = definitions.find((item) => item.key === selectedKey) ?? definitions[0];

  return (
    <div className="sql-lab-page">
      <header className="page-header sql-lab-header">
        <div>
          <p className="eyebrow subtle">Bases de Datos Avanzadas · SOF-008</p>
          <h2>Laboratorio SQL</h2>
          <p className="sql-lab-intro">
            Consulta el SQL original del proyecto y ejecuta su equivalente moderno
            sobre ArchiveCoreDb. Las consultas disponibles son predefinidas y de solo lectura.
          </p>
        </div>
        <span className="status-pill"><span className="status-dot" />Modo seguro</span>
      </header>

      {definitionsQuery.isLoading && <PageState text="Cargando consultas académicas…" />}
      {definitionsQuery.error && <PageState text="No se pudo cargar el laboratorio SQL." />}

      {!definitionsQuery.isLoading && !definitionsQuery.error && (
        <>
          <section className="sql-query-selector">
            {definitions.map((query) => (
              <button
                key={query.key}
                className={query.key === selectedKey ? "active" : ""}
                onClick={() => setSelectedKey(query.key)}
              >
                <Icon name="database" />
                <span>{query.title}</span>
              </button>
            ))}
          </section>

          {selected && (
            <section className="sql-lab-grid">
              <article className="sql-query-card legacy">
                <div className="sql-card-head">
                  <div>
                    <span className="sql-badge">LEGACY</span>
                    <h3>Consulta original</h3>
                  </div>
                  <span>DataBaseProject</span>
                </div>
                <pre className="sql-code"><code>{selected.legacySql}</code></pre>
                <p>
                  Consulta preservada del proyecto académico original desarrollado
                  para Bases de Datos Avanzadas.
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
                <pre className="sql-code"><code>{selected.modernSql}</code></pre>
                <p>{selected.description}</p>
                <button
                  className="primary compact"
                  disabled={executionQuery.isFetching}
                  onClick={() => executionQuery.refetch()}
                >
                  {executionQuery.isFetching ? "Ejecutando…" : "Ejecutar consulta"}
                </button>
              </article>
            </section>
          )}

          <section className="sql-result-card">
            <div className="records-panel-head">
              <div>
                <h3>Resultado</h3>
                <span>Datos reales leídos desde SQL Server</span>
              </div>
              {executionQuery.data && (
                <span className="sql-row-count">
                  {executionQuery.data.rows.length} fila(s)
                </span>
              )}
            </div>

            {executionQuery.isFetching && <PageState text="Ejecutando consulta…" />}
            {executionQuery.error && <PageState text="No se pudo ejecutar la consulta académica." />}
            {!executionQuery.isFetching && !executionQuery.error && !executionQuery.data && (
              <PageState text="Selecciona una consulta y presiona Ejecutar consulta." />
            )}

            {executionQuery.data && (
              <div className="sql-result-table">
                <table>
                  <thead>
                    <tr>
                      {executionQuery.data.columns.map((column) => <th key={column}>{column}</th>)}
                    </tr>
                  </thead>
                  <tbody>
                    {executionQuery.data.rows.map((row, index) => (
                      <tr key={index}>
                        {executionQuery.data!.columns.map((column) => (
                          <td key={column}>
                            {row[column] === null || row[column] === undefined
                              ? "NULL"
                              : typeof row[column] === "boolean"
                                ? (row[column] ? "Sí" : "No")
                                : String(row[column])}
                          </td>
                        ))}
                      </tr>
                    ))}
                    {executionQuery.data.rows.length === 0 && (
                      <tr>
                        <td colSpan={executionQuery.data.columns.length || 1} className="empty">
                          La consulta no devolvió filas.
                        </td>
                      </tr>
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

function PageState({ text }: { text: string }) {
  return <div className="page-state">{text}</div>;
}
