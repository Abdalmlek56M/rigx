import { productPath } from "../../lib/seo/urls.ts";

export interface NotificationEntry {
  kind: string;
  subject: string;
  status?: string | null;
  target: string;
}

const ORDER_STATUSES = ["Confirmed", "Preparing", "Shipped", "Delivered", "Cancelled"] as const;
type OrderStatusKey = (typeof ORDER_STATUSES)[number] | "Updated";

/** The `notifications` message key and its values for one entry; an unknown kind or status still reads sensibly. */
export function notificationText(entry: NotificationEntry): { key: `order.${OrderStatusKey}` | "backInStock" | "requestReply" | "other"; values: Record<string, string> } {
  if (entry.kind === "BackInStock") return { key: "backInStock", values: { item: entry.subject } };
  if (entry.kind === "RequestReply") return { key: "requestReply", values: { code: entry.subject } };
  if (entry.kind !== "OrderStatus") return { key: "other", values: {} };
  const status = ORDER_STATUSES.find((known) => known === entry.status) ?? "Updated";
  return { key: `order.${status}`, values: { reference: entry.subject } };
}

/** Where an entry opens: the order in the account, the store's reply in "my own requests", or the product page (the RX code redirects to its slug). */
export function notificationHref(entry: NotificationEntry): string | null {
  if (entry.kind === "RequestReply") return "/account/requests";
  if (entry.kind === "OrderStatus") return `/account/orders/${encodeURIComponent(entry.target)}`;
  if (entry.kind === "BackInStock") return productPath(encodeURIComponent(entry.target));
  return null;
}

export type NotificationIconKey = "order" | "stock" | "reply" | "other";

/** Which small picture leads an entry in the list. */
export function notificationIconKey(entry: NotificationEntry): NotificationIconKey {
  if (entry.kind === "OrderStatus") return "order";
  if (entry.kind === "BackInStock") return "stock";
  if (entry.kind === "RequestReply") return "reply";
  return "other";
}

/** The badge text: nothing at zero, "9+" above nine (the exact count is in the accessible name). */
export function unreadBadge(count: number): string | null {
  if (count <= 0) return null;
  return count > 9 ? "9+" : String(count);
}
