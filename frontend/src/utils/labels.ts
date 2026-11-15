export const ENTITY_LABELS: Record<string, string> = {
  Records: "Expedientes",
  Documents: "Documentos",
  DocumentVersions: "Versiones de documento",
  Users: "Usuarios",
};

export const ACTION_LABELS: Record<string, string> = {
  INSERT: "Creación",
  UPDATE: "Modificación",
  DELETE: "Eliminación",
  LOGIN: "Inicio de sesión",
  LOGOUT: "Cierre de sesión",
  DOWNLOAD: "Descarga",
  OTHER: "Otra",
};

export const entityLabel = (value: string) => ENTITY_LABELS[value] ?? value;
export const actionLabel = (value: string) => ACTION_LABELS[value] ?? value;
