import { Navigate, Route, Routes } from "react-router-dom";
import { RequireAuth, RequireRole } from "./auth/guards";
import { PageState } from "./components/PageState";
import { Shell } from "./layout/Shell";
import { AuditPage } from "./pages/Audit/AuditPage";
import { DashboardPage } from "./pages/Dashboard/DashboardPage";
import { DocumentDetailPage } from "./pages/Documents/DocumentDetailPage";
import { DocumentsPage } from "./pages/Documents/DocumentsPage";
import { LoginPage } from "./pages/Login/LoginPage";
import { RecordDetailPage } from "./pages/Records/RecordDetailPage";
import { RecordsPage } from "./pages/Records/RecordsPage";
import { SqlLabPage } from "./pages/SqlLab/SqlLabPage";
import { UsersPage } from "./pages/Users/UsersPage";

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/*"
        element={
          <RequireAuth>
            <Shell>
              <Routes>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/records" element={<RecordsPage />} />
                <Route path="/records/:recordId" element={<RecordDetailPage />} />
                <Route path="/documents" element={<DocumentsPage />} />
                <Route path="/documents/:documentId" element={<DocumentDetailPage />} />
                <Route path="/audit" element={<RequireRole roles={["Administrator"]}><AuditPage /></RequireRole>} />
                <Route path="/users" element={<RequireRole roles={["Administrator"]}><UsersPage /></RequireRole>} />
                <Route path="/sql-lab" element={<RequireRole roles={["Administrator"]}><SqlLabPage /></RequireRole>} />
                <Route path="/index.html" element={<Navigate to="/" replace />} />
                <Route path="*" element={<PageState kind="empty" title="Página no encontrada" text="La dirección solicitada no existe en ArchiveCore." />} />
              </Routes>
            </Shell>
          </RequireAuth>
        }
      />
    </Routes>
  );
}
