import type { ReactElement } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { AuthProvider } from "../auth/AuthContext";
import type { AuthResponse, RoleName } from "../api/types";

export type Handler = (request: { url: string; method: string; headers: Headers; body: unknown }) =>
  { status: number; body?: unknown; headers?: Record<string, string> } | undefined;

export type FetchCall = { url: string; method: string; authorization: string | null };

/** Routes fetch calls to the handler; unknown routes return 404 ProblemDetails. */
export function mockFetch(handler: Handler) {
  const calls: FetchCall[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.toString() : input.url;
    const method = (init?.method ?? "GET").toUpperCase();
    const headers = new Headers(init?.headers);
    calls.push({ url, method, authorization: headers.get("Authorization") });
    let body: unknown = undefined;
    if (typeof init?.body === "string") body = JSON.parse(init.body);
    const result = handler({ url, method, headers, body }) ?? { status: 404, body: { title: "No encontrado", status: 404 } };
    const payload = result.body === undefined ? null : typeof result.body === "string" ? result.body : JSON.stringify(result.body);
    return new Response(result.status === 204 ? null : payload, {
      status: result.status,
      headers: { "Content-Type": "application/json", ...result.headers },
    });
  });
  vi.stubGlobal("fetch", fetchMock);
  return { calls, fetchMock };
}

export function authResponse(roles: RoleName[], token = "access-1"): AuthResponse {
  return {
    accessToken: token,
    accessTokenExpiresAtUtc: "2030-01-01T00:00:00Z",
    user: { userId: 7, firstName: "Ana", lastName: "Pérez", email: "ana@example.test", roles },
  };
}

export function emptyPage() {
  return { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };
}

export function dashboardBody() {
  return {
    activeUsers: 3, activeRecords: 5, closedRecords: 1, activeDocuments: 9,
    movementsToday: 2, auditEventsToday: 11, generatedAtUtc: "2026-11-14T12:00:00Z",
  };
}

export function renderApp(ui: ReactElement, path = "/") {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <AuthProvider>{ui}</AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}
