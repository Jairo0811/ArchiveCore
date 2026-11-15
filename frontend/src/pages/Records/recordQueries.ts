import { useQuery, useQueryClient } from "@tanstack/react-query";
import { catalogsApi, recordsApi } from "../../api/endpoints";

export const recordKeys = {
  all: ["records"] as const,
  detail: (recordId: number) => ["records", "detail", recordId] as const,
  history: (recordId: number) => ["records", "history", recordId] as const,
};

export function useRecordStatuses() {
  return useQuery({ queryKey: ["catalogs", "record-statuses"], queryFn: catalogsApi.recordStatuses, staleTime: 5 * 60_000 });
}

export function useRecord(recordId: number) {
  return useQuery({
    queryKey: recordKeys.detail(recordId),
    queryFn: () => recordsApi.get(recordId),
    enabled: Number.isInteger(recordId) && recordId > 0,
  });
}

/** Everything that may show a record (lists, details, history, dashboard counters, audit). */
export function useInvalidateRecords() {
  const queryClient = useQueryClient();
  return async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: recordKeys.all }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["audit"] }),
    ]);
  };
}
