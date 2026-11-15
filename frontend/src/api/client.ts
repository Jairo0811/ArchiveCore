import { session } from "./session";
import type { AuthResponse } from "./types";

export const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, "") ?? "/api/v1";

export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
  correlationId?: string;
};

/** Error carrying the API's RFC 7807 ProblemDetails (Spanish, user-safe messages). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Error HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Field errors keyed by lower-camel field name. */
  get fieldErrors(): Record<string, string> {
    const result: Record<string, string> = {};
    for (const [key, messages] of Object.entries(this.problem.errors ?? {})) {
      const field = key.charAt(0).toLowerCase() + key.slice(1);
      if (messages.length > 0) result[field] = messages[0];
    }
    return result;
  }
}

export class NetworkError extends Error {
  constructor() {
    super("No se pudo conectar con la API de ArchiveCore. Verifica tu conexión e inténtalo de nuevo.");
    this.name = "NetworkError";
  }
}

export function errorMessage(error: unknown, fallback = "Ocurrió un error inesperado."): string {
  if (error instanceof ApiError || error instanceof NetworkError) return error.message;
  return fallback;
}

let refreshInFlight: Promise<string | null> | null = null;

/**
 * Exchanges the HttpOnly refresh cookie for a new access token. Concurrent callers share
 * one request, so a burst of 401s triggers a single rotation (no refresh loops).
 */
export function refreshSession(): Promise<string | null> {
  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const response = await fetch(`${API_BASE}/auth/refresh`, { method: "POST", credentials: "include" });
        if (!response.ok) {
          session.clear(session.getState().status === "authenticated" ? "expired" : undefined);
          return null;
        }
        const auth = (await response.json()) as AuthResponse;
        session.set(auth);
        return auth.accessToken;
      } catch {
        // Network failure: keep the current state; the caller surfaces a connection error.
        return null;
      } finally {
        refreshInFlight = null;
      }
    })();
  }
  return refreshInFlight;
}

type RequestOptions = Omit<RequestInit, "body"> & { body?: unknown; raw?: boolean };

async function send(path: string, options: RequestOptions, token: string | null): Promise<Response> {
  const headers = new Headers(options.headers);
  let body: BodyInit | undefined;

  if (options.body instanceof FormData) {
    body = options.body;
  } else if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(options.body);
  }

  if (token) headers.set("Authorization", `Bearer ${token}`);

  try {
    return await fetch(`${API_BASE}${path}`, { ...options, headers, body, credentials: "include" });
  } catch {
    throw new NetworkError();
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = { status: response.status };
  try {
    const text = await response.text();
    if (text) problem = { ...problem, ...(JSON.parse(text) as ProblemDetails) };
  } catch {
    // Non-JSON error bodies are ignored; the status-based message is used instead.
  }
  if (!problem.detail && !problem.title) problem.detail = defaultMessage(response.status);
  return new ApiError(response.status, problem);
}

function defaultMessage(status: number): string {
  switch (status) {
    case 401: return "Tu sesión expiró. Inicia sesión de nuevo.";
    case 403: return "No tienes permisos para realizar esta acción.";
    case 404: return "El recurso solicitado no existe.";
    case 409: return "La operación entra en conflicto con el estado actual.";
    case 413: return "El archivo supera el tamaño máximo permitido.";
    case 429: return "Demasiadas solicitudes. Espera un momento e inténtalo de nuevo.";
    default: return status >= 500 ? "El servidor no pudo completar la solicitud." : "La solicitud no es válida.";
  }
}

/**
 * Authenticated request. On a 401 it refreshes the session once and retries once;
 * if the refresh fails the session is cleared and the router sends the user to /login.
 */
export async function apiRequest(path: string, options: RequestOptions = {}): Promise<Response> {
  let response = await send(path, options, session.getAccessToken());

  if (response.status === 401 && !path.startsWith("/auth/")) {
    const token = await refreshSession();
    if (token) response = await send(path, options, token);
  }

  if (!response.ok) throw await toApiError(response);
  return response;
}

export async function apiJson<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await apiRequest(path, options);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

/** Anonymous auth call (login/revoke): never triggers the refresh-and-retry flow. */
export async function authRequest<T>(path: string, body?: unknown): Promise<T> {
  const response = await send(path, { method: "POST", body }, null);
  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

/** Extracts the file name from Content-Disposition (RFC 5987 filename* first). */
export function fileNameFromDisposition(header: string | null, fallback: string): string {
  if (!header) return fallback;
  const star = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (star) {
    try {
      return decodeURIComponent(star[1].trim().replace(/^"|"$/g, ""));
    } catch {
      // fall through to the plain filename parameter
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain ? plain[1].trim() : fallback;
}
