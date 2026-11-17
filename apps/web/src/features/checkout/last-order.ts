import type { Schemas } from "@rigx/api-client";

export type PlacedOrder = Schemas["CustomerOrderView"];

const KEY = "rigx.lastOrder";

/** Reads the stored order; anything that is not an order with a reference and a tracking token is ignored. */
export function parseLastOrder(raw: string | null): PlacedOrder | null {
  if (!raw) return null;
  try {
    const value: unknown = JSON.parse(raw);
    if (typeof value !== "object" || value === null) return null;
    const v = value as Record<string, unknown>;
    return typeof v.reference === "string" && typeof v.trackingToken === "string" && Array.isArray(v.items) ? (value as PlacedOrder) : null;
  } catch {
    return null;
  }
}

/** Keeps the placed order for the confirmation page (this tab only); without storage the page shows its short fallback. */
export function saveLastOrder(order: PlacedOrder): void {
  try {
    window.sessionStorage.setItem(KEY, JSON.stringify(order));
  } catch {
    // Storage unavailable: the confirmation page falls back to its generic message.
  }
}

export function readLastOrder(): PlacedOrder | null {
  try {
    return parseLastOrder(window.sessionStorage.getItem(KEY));
  } catch {
    return null;
  }
}
