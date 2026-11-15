import type { ReactNode } from "react";
import { errorMessage } from "../api/client";

type Kind = "loading" | "empty" | "error" | "forbidden" | "info";

export function PageState({
  kind = "info",
  title,
  text,
  action,
  fullScreen = false,
}: {
  kind?: Kind;
  title?: string;
  text: string;
  action?: ReactNode;
  fullScreen?: boolean;
}) {
  const role = kind === "error" ? "alert" : "status";
  return (
    <div className={`page-state ${kind}${fullScreen ? " full-screen" : ""}`} role={role} aria-live="polite">
      {kind === "loading" && <span className="spinner" aria-hidden="true" />}
      {title && <strong>{title}</strong>}
      <p>{text}</p>
      {action}
    </div>
  );
}

export function QueryError({ error, onRetry, text }: { error: unknown; onRetry?: () => void; text?: string }) {
  return (
    <PageState
      kind="error"
      title={text ?? "No se pudo cargar la información"}
      text={errorMessage(error, "Intenta de nuevo en unos segundos.")}
      action={onRetry && <button type="button" className="secondary compact" onClick={onRetry}>Reintentar</button>}
    />
  );
}
