"use client";

import { useSyncExternalStore } from "react";
import { sessionStore, type Session } from "./session-store";

export function useSession(): Session {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.getSnapshot, sessionStore.getServerSnapshot);
}
