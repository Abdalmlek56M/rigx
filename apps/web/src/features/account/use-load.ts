"use client";

import { useCallback, useEffect, useState } from "react";

export type Load<T> = { status: "loading" } | { status: "error" } | { status: "ready"; data: T };

const LOADING = { status: "loading" } as const;

/** Runs `load` on mount (and again after `reload()`); `load` must be stable (module-level or `useCallback`). */
export function useLoad<T>(load: () => Promise<T>): { state: Load<T>; reload: () => void } {
  const [attempt, setAttempt] = useState(0);
  const [result, setResult] = useState<{ attempt: number; state: Load<T> } | null>(null);

  useEffect(() => {
    let current = true;
    load().then(
      (data) => current && setResult({ attempt, state: { status: "ready", data } }),
      () => current && setResult({ attempt, state: { status: "error" } })
    );
    return () => {
      current = false;
    };
  }, [attempt, load]);

  const reload = useCallback(() => setAttempt((value) => value + 1), []);
  return { state: result && result.attempt === attempt ? result.state : LOADING, reload };
}
