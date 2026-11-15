import { createContext, useCallback, useContext, useEffect, useMemo, useSyncExternalStore, type ReactNode } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { refreshSession } from "../api/client";
import { authApi } from "../api/endpoints";
import { session, type SessionState } from "../api/session";
import type { UserSummary } from "../api/types";

type AuthContextValue = {
  state: SessionState;
  user: UserSummary | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const state = useSyncExternalStore(session.subscribe, session.getState, session.getState);

  // Restore the session from the HttpOnly refresh cookie on first load.
  useEffect(() => {
    if (session.getState().status === "loading") {
      void refreshSession().then((token) => {
        if (!token && session.getState().status === "loading") session.clear();
      });
    }
  }, []);

  // Cached data belongs to the previous user: drop it whenever the session ends.
  useEffect(() => {
    if (state.status === "anonymous") queryClient.clear();
  }, [state.status, queryClient]);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await authApi.login(email, password);
    queryClient.clear();
    session.set(auth);
  }, [queryClient]);

  const logout = useCallback(async () => {
    try {
      await authApi.revoke();
    } catch {
      // Best effort: the local session is cleared even if the API is unreachable.
    } finally {
      session.clear("logout");
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ state, user: state.user, login, logout }),
    [state, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside <AuthProvider>.");
  return context;
}
