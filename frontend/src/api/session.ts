import type { AuthResponse, UserSummary } from "./types";

/**
 * In-memory session. The access token is never written to localStorage/sessionStorage;
 * the refresh token lives only in an HttpOnly cookie that JavaScript cannot read.
 * A page reload restores the session through POST /auth/refresh.
 */
export type SessionState =
  | { status: "loading"; user: null }
  | { status: "authenticated"; user: UserSummary }
  | { status: "anonymous"; user: null; reason?: "expired" | "logout" };

type Listener = () => void;

let accessToken: string | null = null;
let state: SessionState = { status: "loading", user: null };
const listeners = new Set<Listener>();

function emit(next: SessionState) {
  state = next;
  listeners.forEach((listener) => listener());
}

export const session = {
  getState: (): SessionState => state,
  getAccessToken: (): string | null => accessToken,
  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  set(auth: AuthResponse) {
    accessToken = auth.accessToken;
    emit({ status: "authenticated", user: auth.user });
  },
  clear(reason?: "expired" | "logout") {
    accessToken = null;
    emit({ status: "anonymous", user: null, reason });
  },
  /** Test helper: resets module state between tests. */
  reset() {
    accessToken = null;
    state = { status: "loading", user: null };
    listeners.clear();
  },
};
