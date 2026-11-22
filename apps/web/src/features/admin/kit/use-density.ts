"use client";

import { useCallback, useSyncExternalStore } from "react";
import { DEFAULT_DENSITY, densityStorageKey, parseDensity, type Density } from "./density-rules";

const listeners = new Set<() => void>();
/** The choice made on this page; it wins over storage, so a browser that blocks storage still follows the switch until the page closes. */
const chosen = new Map<string, Density>();

function read(username: string): Density {
  const made = chosen.get(username);
  if (made) return made;
  try {
    return parseDensity(window.localStorage.getItem(densityStorageKey(username)));
  } catch {
    return DEFAULT_DENSITY;
  }
}

/** The signed-in user's table density and a setter that keeps it in this browser. */
export function useDensity(username: string): readonly [Density, (next: Density) => void] {
  const density = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => read(username),
    () => DEFAULT_DENSITY
  );
  const set = useCallback((next: Density) => {
    chosen.set(username, next);
    try {
      window.localStorage.setItem(densityStorageKey(username), next);
    } catch {
      // Blocked storage: the choice lasts until the page is closed.
    }
    listeners.forEach((listener) => listener());
  }, [username]);
  return [density, set];
}
