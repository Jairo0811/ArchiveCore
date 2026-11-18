import { NavLink } from "react-router-dom";
import type { UserSummary } from "../api/types";
import { Icon } from "../components/Icon";
import { can, roleLabel } from "../auth/permissions";

type NavItem = { to: string; label: string; icon: string; end?: boolean; visible: boolean };

export function Sidebar({
  user,
  open,
  onNavigate,
  onLogout,
  loggingOut,
}: {
  user: UserSummary;
  open: boolean;
  onNavigate: () => void;
  onLogout: () => void;
  loggingOut: boolean;
}) {
  const initials = `${user.firstName[0] ?? ""}${user.lastName[0] ?? ""}`.toUpperCase();
  const items: NavItem[] = [
    { to: "/", label: "Dashboard", icon: "home", end: true, visible: true },
    { to: "/records", label: "Expedientes", icon: "folder", visible: true },
    { to: "/documents", label: "Documentos", icon: "file", visible: true },
    { to: "/audit", label: "Auditoría", icon: "chart", visible: can.administer(user) },
    { to: "/users", label: "Usuarios", icon: "users", visible: can.administer(user) },
    { to: "/sql-lab", label: "SQL Lab (SOF-008)", icon: "database", visible: can.administer(user) },
  ];

  return (
    <aside className={`sidebar${open ? " open" : ""}`} id="app-sidebar">
      <div className="sidebar-brand">
        <picture><source srcSet="/archivecore-icon.webp" type="image/webp" /><img src="/archivecore-icon.png" alt="" width="56" height="56" /></picture>
        <div>
          <strong>ArchiveCore</strong>
          <small>Control documental</small>
        </div>
      </div>

      <nav aria-label="Navegación principal">
        {items.filter((item) => item.visible).map((item) => (
          <NavLink key={item.to} to={item.to} end={item.end} onClick={onNavigate}>
            <Icon name={item.icon} /> <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="sidebar-footer">
        <div className="sidebar-user">
          <div className="avatar" aria-hidden="true">{initials}</div>
          <div className="sidebar-user-copy">
            <strong>{user.firstName} {user.lastName}</strong>
            <small title={user.roles.map(roleLabel).join(" · ")}>{user.roles.map(roleLabel).join(" · ")}</small>
          </div>
        </div>

        <button type="button" className="logout-button" onClick={onLogout} disabled={loggingOut}>
          <Icon name="logout" /> <span>{loggingOut ? "Cerrando sesión…" : "Cerrar sesión"}</span>
        </button>

        <p className="sidebar-slogan">DOCUMENTOS HOY,<br />INSTITUCIONES MAÑANA</p>
      </div>
    </aside>
  );
}
