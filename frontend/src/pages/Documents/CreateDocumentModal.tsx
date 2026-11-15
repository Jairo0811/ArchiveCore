import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { ApiError, errorMessage } from "../../api/client";
import { documentsApi } from "../../api/endpoints";
import { Field } from "../../components/Field";
import { Modal } from "../../components/Modal";
import { ACCEPTED_EXTENSIONS, UPLOAD_HINT, useDocumentCategories, useInvalidateDocuments } from "./documentQueries";

/** Creates the document metadata and, optionally, uploads its first version in the same flow. */
export function CreateDocumentModal({ recordId, recordLabel, onClose }: { recordId: number; recordLabel: string; onClose: () => void }) {
  const navigate = useNavigate();
  const invalidate = useInvalidateDocuments();
  const categories = useDocumentCategories();
  const [title, setTitle] = useState("");
  const [documentNumber, setDocumentNumber] = useState("");
  const [description, setDescription] = useState("");
  const [categoryId, setCategoryId] = useState<number | "">("");
  const [file, setFile] = useState<File | null>(null);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [partialDocumentId, setPartialDocumentId] = useState<number | null>(null);

  const mutation = useMutation({
    mutationFn: async () => {
      const created = await documentsApi.create({
        recordId,
        documentCategoryId: Number(categoryId),
        title: title.trim(),
        documentNumber: documentNumber.trim() || null,
        description: description.trim() || null,
      });
      if (file) {
        try {
          await documentsApi.uploadVersion(created.documentId, file);
        } catch (error) {
          setPartialDocumentId(created.documentId);
          throw error;
        }
      }
      return created;
    },
    onSuccess: async (created) => {
      await invalidate();
      navigate(`/documents/${created.documentId}`);
    },
    onError: async (error) => {
      setErrors(error instanceof ApiError ? error.fieldErrors : {});
      await invalidate();
    },
  });

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (!categoryId) next.documentCategoryId = "Selecciona una categoría.";
    if (title.trim().length < 3) next.title = "Indica un título de al menos 3 caracteres.";
    setErrors(next);
    if (Object.keys(next).length === 0) mutation.mutate();
  };

  return (
    <Modal title="Nuevo documento" description={`Expediente: ${recordLabel}`} onClose={onClose} busy={mutation.isPending}>
      {partialDocumentId ? (
        <div className="form-grid">
          <p className="form-error" role="alert">
            El documento se creó, pero el archivo no pudo subirse: {errorMessage(mutation.error)}
          </p>
          <div className="form-actions">
            <button type="button" className="primary" onClick={() => navigate(`/documents/${partialDocumentId}`)}>
              Ir al documento y reintentar la carga
            </button>
          </div>
        </div>
      ) : (
        <form className="form-grid" onSubmit={submit} noValidate>
          <Field label="Categoría" required error={errors.documentCategoryId}>
            <select value={categoryId} onChange={(event) => setCategoryId(event.target.value ? Number(event.target.value) : "")} disabled={categories.isLoading}>
              <option value="">{categories.isLoading ? "Cargando…" : "Selecciona una categoría"}</option>
              {(categories.data ?? []).map((category) => (
                <option key={category.documentCategoryId} value={category.documentCategoryId}>{category.name}</option>
              ))}
            </select>
          </Field>
          <Field label="Título" required error={errors.title}>
            <input value={title} onChange={(event) => setTitle(event.target.value)} maxLength={200} />
          </Field>
          <Field label="Número de documento" hint="Opcional." error={errors.documentNumber}>
            <input value={documentNumber} onChange={(event) => setDocumentNumber(event.target.value)} maxLength={60} />
          </Field>
          <Field label="Descripción" error={errors.description}>
            <textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={1000} rows={3} />
          </Field>
          <Field label="Archivo (versión 1)" hint={`Opcional. ${UPLOAD_HINT}`} error={errors.file}>
            <input type="file" accept={ACCEPTED_EXTENSIONS} onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
          </Field>
          {mutation.error && Object.keys(errors).length === 0 && <p className="form-error" role="alert">{errorMessage(mutation.error)}</p>}
          <div className="form-actions">
            <button type="button" className="secondary" onClick={onClose} disabled={mutation.isPending}>Cancelar</button>
            <button type="submit" className="primary" disabled={mutation.isPending}>
              {mutation.isPending ? (file ? "Creando y subiendo…" : "Creando…") : "Crear documento"}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
