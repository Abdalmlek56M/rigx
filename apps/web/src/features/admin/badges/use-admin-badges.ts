"use client";

import { useEffect, useState } from "react";
import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { isBadgeArea, parseSeen, seenKey, visibleCounts, type BadgeArea, type BadgeCounts } from "./badge-rules";

const REFRESH_MS = 60_000;
const NONE: BadgeCounts = { orders: null, requests: null };

const read = (key: string) => {
  try { return parseSeen(window.localStorage.getItem(key)); } catch { return null; }
};
const write = (key: string, value: string) => {
  try { window.localStorage.setItem(key, value); } catch { /* private mode: the badge simply shows again next time */ }
};

const fetchBadges = (ordersSince: string | null, requestsSince: string | null) =>
  required(unwrap(browserApi().GET("/api/v1/admin/badges", {
    params: { query: { ordersSince: ordersSince ?? undefined, requestsSince: requestsSince ?? undefined } }
  })));

/**
 * New orders / requests since this person last opened each section. The counts refresh every minute while the page is open and when the
 * section changes; opening a section marks it seen (at the server's time of the count), so its badge clears and stays cleared.
 */
export function useAdminBadges(username: string | null, viewing: string | null): BadgeCounts {
  const [counts, setCounts] = useState<BadgeCounts>(NONE);

  useEffect(() => {
    if (!username) return;
    let alive = true;
    async function refresh(user: string) {
      try {
        const result = await fetchBadges(read(seenKey(user, "orders")), read(seenKey(user, "requests")));
        if (!alive) return;
        if (isBadgeArea(viewing)) write(seenKey(user, viewing as BadgeArea), result.asOf);
        setCounts({ orders: result.orders ?? null, requests: result.requests ?? null });
      } catch {
        // A failed refresh keeps the last numbers; the badge is a convenience, never a blocker.
      }
    }
    void refresh(username);
    const timer = window.setInterval(() => { if (document.visibilityState === "visible") void refresh(username); }, REFRESH_MS);
    return () => { alive = false; window.clearInterval(timer); };
  }, [username, viewing]);

  return visibleCounts(counts, viewing);
}
