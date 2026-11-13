"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { getCurrentUser, type CurrentUser } from "@/lib/api-client";

export type SessionStatus = "loading" | "authenticated" | "anonymous" | "unavailable";

type SessionContextValue = {
  status: SessionStatus;
  user: CurrentUser | null;
  refresh: () => Promise<CurrentUser | null>;
  markSignedOut: () => void;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<SessionStatus>("loading");
  const [user, setUser] = useState<CurrentUser | null>(null);

  const refresh = useCallback(async () => {
    try {
      const nextUser = await getCurrentUser();
      setUser(nextUser);
      setStatus(nextUser ? "authenticated" : "anonymous");
      return nextUser;
    } catch {
      setUser(null);
      setStatus("unavailable");
      return null;
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo<SessionContextValue>(() => ({
    status,
    user,
    refresh,
    markSignedOut: () => {
      setUser(null);
      setStatus("anonymous");
    },
  }), [refresh, status, user]);

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): SessionContextValue {
  const value = useContext(SessionContext);
  if (!value) throw new Error("useSession must be used inside SessionProvider.");
  return value;
}
