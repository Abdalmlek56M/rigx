"use client";

import { useCallback, useMemo, useSyncExternalStore } from "react";
import { navCollapseKey, parseCollapsed, toggleCollapsed } from "./nav-collapse";

const listeners = new Set<() => void>();
/** The newest choice made on this page; it wins over storage, so a browser that blocks storage still follows the click until the page closes. */
const chosen = new Map<string, string>();

function readRaw(key: string): string | null {
  const made = chosen.get(key);
  if (made !== undefined) return made;
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** The sidebar groups the signed-in user folded, and a toggle that remembers the choice in this browser. */
export function useCollapsedGroups(username: string): readonly [readonly string[], (group: string) => void] {
  const key = navCollapseKey(username);
  const raw = useSyncExternalStore(subscribe, () => readRaw(key), () => null);
  const collapsed = useMemo(() => parseCollapsed(raw), [raw]);
  const toggle = useCallback((group: string) => {
    const next = JSON.stringify(toggleCollapsed(parseCollapsed(readRaw(key)), group));
    chosen.set(key, next);
    try {
      window.localStorage.setItem(key, next);
    } catch {
      // Blocked storage: the fold lasts until the page is closed.
    }
    listeners.forEach((listener) => listener());
  }, [key]);
  return [collapsed, toggle];
}
