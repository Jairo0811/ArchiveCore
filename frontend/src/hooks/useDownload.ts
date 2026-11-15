import { useState } from "react";
import { errorMessage } from "../api/client";
import { documentsApi } from "../api/endpoints";

/** Authenticated download: fetch with the bearer token, then hand the blob to the browser. */
export function useDownload() {
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const download = async (documentId: number, version?: number) => {
    const key = `${documentId}:${version ?? "current"}`;
    setBusyKey(key);
    setError(null);
    try {
      const { blob, fileName } = await documentsApi.download(documentId, version);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileName;
      anchor.rel = "noopener";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (err) {
      setError(errorMessage(err, "No se pudo descargar el archivo."));
    } finally {
      setBusyKey(null);
    }
  };

  return { download, busyKey, error, clearError: () => setError(null) };
}
