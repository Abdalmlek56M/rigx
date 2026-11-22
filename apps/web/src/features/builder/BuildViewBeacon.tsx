"use client";

import { useEffect } from "react";
import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";

/**
 * Tells the API that a person opened this shared build, once per page load. It renders nothing. Counting happens on the server (one visit per
 * visitor per day, never a robot or the build's owner), and a failure is ignored: a missed view must never disturb the page.
 */
export function BuildViewBeacon({ token }: { token: string }) {
  useEffect(() => {
    void unwrap(browserApi().POST("/api/v1/builder/saved/{token}/views", { params: { path: { token } } })).catch(() => undefined);
  }, [token]);
  return null;
}
