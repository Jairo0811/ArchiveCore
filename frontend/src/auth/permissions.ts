import type { RoleName, UserSummary } from "../api/types";

export const ROLE_LABELS: Record<RoleName, string> = {
  Administrator: "Administrador",
  RecordsManager: "Gestor de expedientes",
  User: "Usuario (lectura)",
};

export function roleLabel(role: string): string {
  return ROLE_LABELS[role as RoleName] ?? role;
}

export function hasRole(user: UserSummary | null, ...roles: RoleName[]): boolean {
  return !!user && user.roles.some((role) => roles.includes(role));
}

/** UI hints only: the API enforces the same rules with its authorization policies. */
export const can = {
  manageRecords: (user: UserSummary | null) => hasRole(user, "Administrator", "RecordsManager"),
  administer: (user: UserSummary | null) => hasRole(user, "Administrator"),
};
