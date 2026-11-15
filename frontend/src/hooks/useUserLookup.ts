import { useQuery } from "@tanstack/react-query";
import { catalogsApi } from "../api/endpoints";

export const USERS_LOOKUP_KEY = ["catalogs", "users"] as const;

/** Active users (id + full name) for responsible/assignee pickers and filters. */
export function useUserLookup() {
  return useQuery({ queryKey: USERS_LOOKUP_KEY, queryFn: catalogsApi.users, staleTime: 60_000 });
}
