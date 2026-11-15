import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { errorMessage } from "../../api/client";
import { recordsApi } from "../../api/endpoints";
import type { RecordDetails } from "../../api/types";
import { Field } from "../../components/Field";
import { ConfirmDialog, Modal } from "../../components/Modal";
import { UserSelect } from "../../components/UserSelect";
import { useInvalidateRecords } from "./recordQueries";

export function TransferModal({ record, onClose }: { record: RecordDetails; onClose: () => void }) {
  const invalidate = useInvalidateRecords();
  const [toUserId, setToUserId] = useState<number | null>(null);
  const [notes, setNotes] = useState("");
  const [fieldError, setFieldError] = useState<string>();
  const isAssignment = record.assignedToUserId === null;

  const mutation = useMutation({
    mutationFn: () => recordsApi.transfer(record.recordId, toUserId!, notes.trim() || undefined),
    onSuccess: async () => {
      await invalidate();
      onClose();
    },
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (!toUserId) {
      setFieldError("Selecciona el usuario destino.");
      return;
    }
    setFieldError(undefined);
    mutation.mutate();
  };

  return (
    <Modal
      title={isAssignment ? `Asignar ${record.recordNumber}` : `Transferir ${record.recordNumber}`}
      description={isAssignment
        ? "El expediente no tiene responsable. Se registrará un movimiento de asignación."
        : `Responsable actual: ${record.assignedToName}. Se registrará un movimiento de transferencia.`}
      onClose={onClose}
      busy={mutation.isPending}
    >
      <form className="form-grid" onSubmit={submit} noValidate>
        <Field label={isAssignment ? "Asignar a" : "Transferir a"} required error={fieldError}>
          <UserSelect value={toUserId} onChange={setToUserId} emptyLabel="Selecciona un usuario" excludeUserId={record.assignedToUserId} />
        </Field>
        <Field label="Notas" hint="Opcional. Quedan en el historial de movimientos.">
          <textarea value={notes} onChange={(event) => setNotes(event.target.value)} maxLength={1000} rows={3} />
        </Field>
        {mutation.error && <p className="form-error" role="alert">{errorMessage(mutation.error)}</p>}
        <div className="form-actions">
          <button type="button" className="secondary" onClick={onClose} disabled={mutation.isPending}>Cancelar</button>
          <button type="submit" className="primary" disabled={mutation.isPending}>
            {mutation.isPending ? "Registrando…" : isAssignment ? "Asignar" : "Transferir"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export function CloseRecordDialog({ record, onClose }: { record: RecordDetails; onClose: () => void }) {
  const invalidate = useInvalidateRecords();
  const [notes, setNotes] = useState("");
  const mutation = useMutation({
    mutationFn: () => recordsApi.close(record.recordId, notes.trim() || undefined),
    onSuccess: async () => {
      await invalidate();
      onClose();
    },
  });

  return (
    <ConfirmDialog
      title={`Cerrar ${record.recordNumber}`}
      message="Un expediente cerrado no admite cambios, transferencias ni nuevas versiones de documentos. Esta acción no se puede deshacer."
      confirmLabel="Cerrar expediente"
      busy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      onConfirm={() => mutation.mutate()}
      onClose={onClose}
    >
      <Field label="Notas de cierre" hint="Opcional.">
        <textarea value={notes} onChange={(event) => setNotes(event.target.value)} maxLength={1000} rows={3} />
      </Field>
    </ConfirmDialog>
  );
}
