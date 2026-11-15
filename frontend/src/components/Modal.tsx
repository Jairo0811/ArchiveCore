import { useEffect, useId, useRef, type ReactNode } from "react";
import { Icon } from "./Icon";

/**
 * Accessible dialog built on the native <dialog> element: focus trap, Esc to close and
 * focus restoration are provided by the browser.
 */
export function Modal({
  title,
  description,
  onClose,
  children,
  footer,
  busy = false,
}: {
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  busy?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (typeof dialog.showModal === "function" && !dialog.open) dialog.showModal();
    else dialog.setAttribute("open", "");
    return () => {
      if (typeof dialog.close === "function" && dialog.open) dialog.close();
    };
  }, []);

  return (
    <dialog
      ref={ref}
      className="modal"
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
    >
      <div className="modal-head">
        <div>
          <h3 id={titleId}>{title}</h3>
          {description && <p id={descriptionId}>{description}</p>}
        </div>
        <button type="button" className="icon-button" onClick={onClose} disabled={busy} aria-label="Cerrar">
          <Icon name="close" />
        </button>
      </div>
      <div className="modal-body">{children}</div>
      {footer && <div className="modal-footer">{footer}</div>}
    </dialog>
  );
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel,
  tone = "danger",
  busy,
  error,
  onConfirm,
  onClose,
  children,
}: {
  title: string;
  message: string;
  confirmLabel: string;
  tone?: "danger" | "primary";
  busy: boolean;
  error?: string | null;
  onConfirm: () => void;
  onClose: () => void;
  children?: ReactNode;
}) {
  return (
    <Modal
      title={title}
      onClose={onClose}
      busy={busy}
      footer={
        <>
          <button type="button" className="secondary" onClick={onClose} disabled={busy}>Cancelar</button>
          <button type="button" className={tone === "danger" ? "danger" : "primary"} onClick={onConfirm} disabled={busy} autoFocus>
            {busy ? "Procesando…" : confirmLabel}
          </button>
        </>
      }
    >
      <p className="modal-message">{message}</p>
      {children}
      {error && <p className="form-error" role="alert">{error}</p>}
    </Modal>
  );
}
