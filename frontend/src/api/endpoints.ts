import { apiJson, apiRequest, authRequest, fileNameFromDisposition } from "./client";
import type {
  AuditEventSummary,
  AuthResponse,
  DashboardSummary,
  DocumentCategoryItem,
  DocumentDetails,
  DocumentSummary,
  MovementSummary,
  PagedResult,
  RecordDetails,
  RecordStatusItem,
  RecordSummary,
  RoleItem,
  RoleName,
  SqlLabQueryDefinition,
  SqlLabQueryResult,
  UserListItem,
  UserLookupItem,
} from "./types";

function query(params: Record<string, string | number | boolean | null | undefined>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
}

export const authApi = {
  login: (email: string, password: string) => authRequest<AuthResponse>("/auth/login", { email, password }),
  revoke: () => authRequest<void>("/auth/revoke"),
};

export type RecordFilters = {
  search?: string;
  statusId?: number | null;
  assignedToUserId?: number | null;
  unassigned?: boolean;
  page?: number;
  pageSize?: number;
};

export type CreateRecordInput = {
  recordNumber: string;
  title: string;
  description?: string | null;
  assignedToUserId?: number | null;
};

export type UpdateRecordInput = {
  title: string;
  description?: string | null;
  recordStatusId: number;
  rowVersion: string;
};

export const recordsApi = {
  list: (filters: RecordFilters) =>
    apiJson<PagedResult<RecordSummary>>(`/records${query({ ...filters })}`),
  get: (recordId: number) => apiJson<RecordDetails>(`/records/${recordId}`),
  create: (input: CreateRecordInput) => apiJson<RecordDetails>("/records", { method: "POST", body: input }),
  update: (recordId: number, input: UpdateRecordInput) =>
    apiJson<RecordDetails>(`/records/${recordId}`, { method: "PUT", body: input }),
  history: (recordId: number) => apiJson<MovementSummary[]>(`/records/${recordId}/workflow/history`),
  transfer: (recordId: number, toUserId: number, notes?: string) =>
    apiJson<void>(`/records/${recordId}/workflow/transfer`, { method: "POST", body: { toUserId, notes } }),
  close: (recordId: number, notes?: string) =>
    apiJson<void>(`/records/${recordId}/workflow/close`, { method: "POST", body: { notes } }),
};

export type CreateDocumentInput = {
  recordId: number;
  documentCategoryId: number;
  title: string;
  documentNumber?: string | null;
  description?: string | null;
};

export const documentsApi = {
  listByRecord: (recordId: number) => apiJson<DocumentSummary[]>(`/documents/record/${recordId}`),
  get: (documentId: number) => apiJson<DocumentDetails>(`/documents/${documentId}`),
  create: (input: CreateDocumentInput) => apiJson<DocumentDetails>("/documents", { method: "POST", body: input }),
  uploadVersion: (documentId: number, file: File) => {
    const form = new FormData();
    form.append("file", file, file.name);
    return apiJson<DocumentDetails>(`/documents/${documentId}/versions`, { method: "POST", body: form });
  },
  remove: (documentId: number) => apiJson<void>(`/documents/${documentId}`, { method: "DELETE" }),
  download: async (documentId: number, version?: number): Promise<{ blob: Blob; fileName: string }> => {
    const response = await apiRequest(`/documents/${documentId}/download${query({ version })}`);
    const fileName = fileNameFromDisposition(response.headers.get("Content-Disposition"), `documento-${documentId}`);
    return { blob: await response.blob(), fileName };
  },
};

export const catalogsApi = {
  recordStatuses: () => apiJson<RecordStatusItem[]>("/catalogs/record-statuses"),
  documentCategories: () => apiJson<DocumentCategoryItem[]>("/catalogs/document-categories"),
  users: () => apiJson<UserLookupItem[]>("/catalogs/users"),
};

export const dashboardApi = {
  summary: () => apiJson<DashboardSummary>("/dashboard"),
  /** True only when the API reports its database health check as Healthy. */
  health: async (): Promise<boolean> => {
    try {
      const response = await fetch("/health", { cache: "no-store" });
      return response.ok && (await response.text()).trim() === "Healthy";
    } catch {
      return false;
    }
  },
};

export type AuditFilters = {
  entityName?: string;
  entityKey?: string;
  userId?: number | null;
  actionType?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
};

export const auditApi = {
  search: (filters: AuditFilters) => apiJson<PagedResult<AuditEventSummary>>(`/audit${query({ ...filters })}`),
};

export type CreateUserInput = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roles: RoleName[];
  isActive: boolean;
};

export const usersApi = {
  list: () => apiJson<UserListItem[]>("/users"),
  roles: () => apiJson<RoleItem[]>("/users/roles"),
  create: (input: CreateUserInput) => apiJson<UserListItem>("/users", { method: "POST", body: input }),
  setStatus: (userId: number, isActive: boolean) =>
    apiJson<UserListItem>(`/users/${userId}/status`, { method: "PATCH", body: { isActive } }),
};

export const sqlLabApi = {
  queries: () => apiJson<SqlLabQueryDefinition[]>("/academic/sql-lab/queries"),
  execute: (key: string) => apiJson<SqlLabQueryResult>(`/academic/sql-lab/queries/${encodeURIComponent(key)}`),
};
