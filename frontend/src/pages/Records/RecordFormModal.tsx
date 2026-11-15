import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { ApiError, errorMessage } from "../../api/client";
import { recordsApi } from "../../api/endpoints";
import type { RecordDetails } from "../../api/types";
import { Field } from "../../components/Field";
import { Modal } from "../../components/Modal";
import { UserSelect } from "../../components/UserSelect";
import { useInvalidateRecords, useRecordStatuses } from "./recordQueries";

const RECORD_NUMBER = /^[A-Za-z0-9][A-Za-z0-9\-_/.]*$/;

export function CreateRecordModal({ onClose, onCreated }: { onClose: () => void; onCreated: (record: RecordDetails) => void }) {
  const invalidate = useInvalidateRecords();
  const [recordNumber, setRecordNumber] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [assignedToUserId, setAssignedToUserId] = useState<number | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: () => recordsApi.create({
      recordNumber: recordNumber.trim(),
      title: title.trim(),
      description: description.trim() || null,
      assignedToUserId,
    }),
    onSuccess: async (record) => {
      await invalidate();
      onCreated(record);
    },
    onError: (error) => setErrors(error instanceof ApiError ? error.fieldErrors : {}),
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (recordNumber.trim().length < 3) next.recordNumber = "Indica un código de al menos 3 caracteres.";
    else if (!RECORD_NUMBER.test(recordNumber.trim())) next.recordNumber = "Usa letras, números, guion, guion bajo, punto o barra (sin espacios).";
    if (title.trim().length < 3) next.title = "Indica un título de al menos 3 caracteres.";
    setErrors(next);
    if (Object.keys(next).length === 0) mutation.mutate();
  };

  const formError = mutation.error && Object.keys(errors).length === 0 ? errorMessage(mutation.error) : null;

  return (
    <Modal
      title="Nuevo expediente"
      description="El expediente se crea en estado Abierto. Si indicas un responsable se registra la asignación inicial."
      onClose={onClose}
      busy={mutation.isPending}
    >
      <form className="form-grid" onSubmit={submit} noValidate>
        <Field label="Código" required hint="Ejemplo: EXP-2026-0001" error={errors.recordNumber}>
          <input value={recordNumber} onChange={(event) => setRecordNumber(event.target.value)} maxLength={40} autoFocus />
        </Field>
        <Field label="Título" required error={errors.title}>
          <input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} />
        </Field>
        <Field label="Descripción" error={errors.description}>
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={1000} rows={3} />
        </Field>
        <Field label="Responsable inicial" hint="Opcional. Solo usuarios activos." error={errors.assignedToUserId}>
          <UserSelect value={assignedToUserId} onChange={setAssignedToUserId} emptyLabel="Sin asignar" />
        </Field>
        {formError && <p className="form-error" role="alert">{formError}</p>}
        <div className="form-actions">
          <button type="button" className="secondary" onClick={onClose} disabled={mutation.isPending}>Cancelar</button>
          <button type="submit" className="primary" disabled={mutation.isPending}>
            {mutation.isPending ? "Creando…" : "Crear expediente"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export function EditRecordModal({ record, onClose }: { record: RecordDetails; onClose: () => void }) {
  const invalidate = useInvalidateRecords();
  const statuses = useRecordStatuses();
  const [title, setTitle] = useState(record.title);
  const [description, setDescription] = useState(record.description ?? "");
  const [recordStatusId, setRecordStatusId] = useState(record.recordStatusId);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: () => recordsApi.update(record.recordId, {
      title: title.trim(),
      description: description.trim() || null,
      recordStatusId,
      rowVersion: record.rowVersion,
    }),
    onSuccess: async () => {
      await invalidate();
      onClose();
    },
    onError: (error) => setErrors(error instanceof ApiError ? error.fieldErrors : {}),
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (title.trim().length < 3) {
      setErrors({ title: "Indica un título de al menos 3 caracteres." });
      return;
    }
    setErrors({});
    mutation.mutate();
  };

  const isConflict = mutation.error instanceof ApiError && mutation.error.status === 409;
  const formError = mutation.error && Object.keys(errors).length === 0 ? errorMessage(mutation.error) : null;
  const editableStatuses = (statuses.data ?? []).filter((status) => !status.isFinal);

  return (
    <Modal title={`Editar ${record.recordNumber}`} description="Para cerrar el expediente utiliza la acción Cerrar." onClose={onClose} busy={mutation.isPending}>
      <form className="form-grid" onSubmit={submit} noValidate>
        <Field label="Título" required error={errors.title}>
          <input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} autoFocus />
        </Field>
        <Field label="Descripción" error={errors.description}>
          <textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={1000} rows={3} />
        </Field>
        <Field label="Estado" error={errors.recordStatusId}>
          <select value={recordStatusId} onChange={(event) => setRecordStatusId(Number(event.target.value))} disabled={statuses.isLoading}>
            {editableStatuses.map((status) => (
              <option key={status.recordStatusId} value={status.recordStatusId}>{status.name}</option>
            ))}
          </select>
        </Field>
        {formError && (
          <p className="form-error" role="alert">
            {formError}
            {isConflict && " Cierra este diálogo para ver la versión actual."}
          </p>
        )}
        <div className="form-actions">
          <button type="button" className="secondary" onClick={onClose} disabled={mutation.isPending}>Cancelar</button>
          <button type="submit" className="primary" disabled={mutation.isPending || isConflict}>
            {mutation.isPending ? "Guardando…" : "Guardar cambios"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
