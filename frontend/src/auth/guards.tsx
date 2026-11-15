import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import type { RoleName } from "../api/types";
import { PageState } from "../components/PageState";
import { useAuth } from "./AuthContext";
import { hasRole } from "./permissions";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { state } = useAuth();
  const location = useLocation();

  if (state.status === "loading") return <PageState kind="loading" text="Restaurando sesión…" fullScreen />;
  if (state.status === "anonymous") {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }
  return <>{children}</>;
}

export function RequireRole({ roles, children }: { roles: RoleName[]; children: ReactNode }) {
  const { user } = useAuth();
  if (!hasRole(user, ...roles)) {
    return (
      <PageState
        kind="forbidden"
        title="Acceso restringido"
        text="Tu rol no tiene acceso a esta sección. Si necesitas acceso, contacta a un administrador."
      />
    );
  }
  return <>{children}</>;
}
