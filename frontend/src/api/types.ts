export type RoleName = "Administrator" | "RecordsManager" | "User";

export type UserSummary = {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  roles: RoleName[];
};

export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  user: UserSummary;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type DashboardSummary = {
  activeUsers: number;
  activeRecords: number;
  closedRecords: number;
  activeDocuments: number;
  movementsToday: number;
  auditEventsToday: number;
  generatedAtUtc: string;
};

export type RecordStatusItem = { recordStatusId: number; code: string; name: string; isFinal: boolean };

export type RecordSummary = {
  recordId: number;
  recordNumber: string;
  title: string;
  statusCode: string;
  status: string;
  isFinal: boolean;
  assignedToUserId: number | null;
  assignedToName: string | null;
  openedAtUtc: string;
  closedAtUtc: string | null;
};

export type RecordDetails = RecordSummary & {
  description: string | null;
  recordStatusId: number;
  createdByUserId: number;
  createdByName: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  rowVersion: string;
};

export type MovementSummary = {
  recordMovementId: number;
  recordId: number;
  movementTypeCode: string;
  movementType: string;
  fromUserId: number | null;
  fromUserName: string | null;
  toUserId: number | null;
  toUserName: string | null;
  performedByUserId: number;
  performedByName: string;
  notes: string | null;
  performedAtUtc: string;
};

export type DocumentCategoryItem = { documentCategoryId: number; code: string; name: string };

export type DocumentSummary = {
  documentId: number;
  recordId: number;
  title: string;
  documentNumber: string | null;
  category: string;
  currentVersion: number;
  currentFileName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type DocumentVersionItem = {
  documentVersionId: number;
  versionNumber: number;
  originalFileName: string;
  fileExtension: string | null;
  mimeType: string | null;
  fileSizeBytes: number;
  sha256Hash: string;
  uploadedByUserId: number;
  uploadedByName: string;
  uploadedAtUtc: string;
  isCurrent: boolean;
};

export type DocumentDetails = {
  documentId: number;
  recordId: number;
  recordNumber: string;
  recordTitle: string;
  recordIsFinal: boolean;
  documentCategoryId: number;
  category: string;
  documentNumber: string | null;
  title: string;
  description: string | null;
  currentVersion: number;
  currentFileName: string | null;
  createdByUserId: number;
  createdByName: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  versions: DocumentVersionItem[];
};

export type AuditEventSummary = {
  auditEventId: number;
  userId: number | null;
  userName: string | null;
  entityName: string;
  entityKey: string;
  actionType: string;
  createdAtUtc: string;
  ipAddress: string | null;
  correlationId: string | null;
};

export type UserLookupItem = { userId: number; fullName: string };

export type UserListItem = {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  roles: RoleName[];
  createdAtUtc: string;
};

export type RoleItem = { roleId: number; name: RoleName; description: string | null };

export type SqlLabQueryDefinition = {
  key: string;
  title: string;
  legacySql: string;
  modernSql: string;
  description: string;
  sourcePath: string;
};

export type SqlLabQueryResult = {
  query: SqlLabQueryDefinition;
  columns: string[];
  rows: Record<string, unknown>[];
};
