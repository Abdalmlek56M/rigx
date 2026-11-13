"use client";

import { useEffect, useRef, useState } from "react";

export function useFragmentToken(): { token: string | null; ready: boolean } {
  const [token, setToken] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const read = useRef(false);

  useEffect(() => {
    if (read.current) return;
    read.current = true;

    const params = new URLSearchParams(window.location.hash.replace(/^#/, ""));
    const value = params.get("token");
    setToken(value);
    setReady(true);

    if (window.location.hash) {
      window.history.replaceState(
        null,
        "",
        `${window.location.pathname}${window.location.search}`,
      );
    }
  }, []);

  return { token, ready };
}
