import { FormEvent, useMemo, useState } from "react";
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
    } catch {
      setError("No pudimos iniciar sesión. Verifica tus credenciales.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-card">
        <div className="brand-mark" aria-hidden="true">
          <span className="brand-folder" />
          <span className="brand-db" />
          <span className="brand-check">✓</span>
        </div>
        <p className="eyebrow">Document & Records Management</p>
        <h1>Archive<span>Core</span></h1>
        <p className="muted">Expedientes, documentos y trazabilidad en una sola plataforma.</p>

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
          <div className="mini-logo">AC</div>
          <div><strong>ArchiveCore</strong><small>Control documental</small></div>
        </div>
        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/records">Expedientes</NavLink>
          <NavLink to="/documents">Documentos</NavLink>
          {auth.user.roles.includes("Administrator") && <NavLink to="/audit">Auditoría</NavLink>}
        </nav>
        <div className="sidebar-user">
          <div className="avatar">{initials}</div>
          <div><strong>{auth.user.firstName} {auth.user.lastName}</strong><small>{auth.user.roles.join(" · ")}</small></div>
          <button className="ghost" onClick={onLogout}>Salir</button>
        </div>
      </aside>
      <main className="content">
        <Routes>
          <Route path="/" element={<Dashboard token={auth.accessToken} />} />
          <Route path="/records" element={<Records token={auth.accessToken} />} />
          <Route path="/documents" element={<Documents token={auth.accessToken} />} />
          <Route path="/audit" element={<Audit token={auth.accessToken} />} />
        </Routes>
      </main>
    </div>
  );
}

function Dashboard({ token }: { token: string }) {
  const { data, isLoading, error } = useQuery({
    queryKey: ["dashboard"],
    queryFn: () => api<DashboardSummary>("/dashboard", {}, token),
  });

  if (isLoading) return <PageState text="Cargando dashboard…" />;
  if (error || !data) return <PageState text="No se pudo cargar el dashboard." />;

  const cards: [string, number][] = [
    ["Expedientes activos", data.activeRecords],
    ["Documentos activos", data.activeDocuments],
    ["Usuarios activos", data.activeUsers],
    ["Movimientos hoy", data.movementsToday],
    ["Expedientes cerrados", data.closedRecords],
    ["Eventos de auditoría hoy", data.auditEventsToday],
  ];

  return (
    <>
      <header className="page-header">
        <div><p className="eyebrow">Overview</p><h2>Centro de control</h2></div>
        <span className="status-pill">● Sistema operativo</span>
      </header>
      <section className="metric-grid">
        {cards.map(([label, value]) => (
          <article className="metric-card" key={label}><span>{label}</span><strong>{value}</strong></article>
        ))}
      </section>
      <section className="panel">
        <p className="eyebrow">ArchiveCore</p>
        <h3>Trazabilidad desde la base de datos</h3>
        <p className="muted">Los cambios críticos quedan respaldados por auditoría SQL Server, versionado documental y movimientos de expediente.</p>
      </section>
    </>
  );
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
        <div><p className="eyebrow">Records</p><h2>Expedientes</h2></div>
        <input className="search" placeholder="Buscar código o título…" value={search}
          onChange={(event) => setSearch(event.target.value)} />
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
                  <td><span className="tag">{record.status}</span></td>
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
        <div><p className="eyebrow">Documents</p><h2>Documentos</h2></div>
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
                  <td><span className="tag">v{document.currentVersion}</span></td>
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
        <div><p className="eyebrow">Traceability</p><h2>Auditoría</h2></div>
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
                  <td><span className="tag">{event.actionType}</span></td>
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

function PageState({ text }: { text: string }) {
  return <div className="page-state">{text}</div>;
}
