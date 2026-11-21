/**
 * The sidebar badges: new work per section since the staff member last opened it. Pure rules (which area shows which count, what
 * is remembered, how a number is shown), so they are tested; the hook in `use-admin-badges.ts` only wires them to the API.
 */
import type { AdminAreaKey } from "../admin-nav";

export interface BadgeCounts {
  orders: number | null;
  requests: number | null;
}

/** The areas that show a badge, and the count each one reads. */
export const BADGE_AREAS = { orders: "orders", requests: "requests" } as const satisfies Partial<Record<AdminAreaKey, keyof BadgeCounts>>;

export type BadgeArea = keyof typeof BADGE_AREAS;

export const isBadgeArea = (key: string | null | undefined): key is BadgeArea => key != null && key in BADGE_AREAS;

/** "3", and "99+" past a hundred so the pill stays small; nothing for zero or no count. */
export function badgeLabel(count: number | null | undefined): string | null {
  if (!count || count < 1) return null;
  return count > 99 ? "99+" : String(count);
}

/** The counts to show: the section being viewed is already seen, so it shows nothing (it is marked seen at the same time). */
export function visibleCounts(counts: BadgeCounts, viewing: string | null | undefined): BadgeCounts {
  return {
    orders: viewing === "orders" ? 0 : counts.orders,
    requests: viewing === "requests" ? 0 : counts.requests
  };
}

/** Where a staff member's last visit to a section is remembered in the browser (per username, so shared computers stay apart). */
export const seenKey = (username: string, area: BadgeArea) => `rigx.admin.seen.${username}.${area}`;

/** The stored moment when it is a real date, else null (nothing stored, or a damaged value: every new item counts again). */
export function parseSeen(value: string | null): string | null {
  return value !== null && !Number.isNaN(Date.parse(value)) ? value : null;
}
