import { useQuery, useQueryClient } from "@tanstack/react-query";
import { catalogsApi } from "../../api/endpoints";

export const ACCEPTED_EXTENSIONS = ".pdf,.png,.jpg,.jpeg,.tif,.tiff,.docx,.xlsx,.pptx,.odt,.ods,.doc,.xls,.ppt,.txt,.csv";
export const UPLOAD_HINT = "PDF, imágenes (PNG, JPG, TIFF), Office/OpenDocument, TXT o CSV. Máximo 20 MB por archivo (límite por defecto).";

export function useDocumentCategories() {
  return useQuery({ queryKey: ["catalogs", "document-categories"], queryFn: catalogsApi.documentCategories, staleTime: 5 * 60_000 });
}

export function useInvalidateDocuments() {
  const queryClient = useQueryClient();
  return async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["documents"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["audit"] }),
    ]);
  };
}
