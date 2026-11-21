/**
 * ADM-Orders: the status workflow, payment gates and list filters, mirroring the server's `OrderRules` so the page offers
 * only the actions the API accepts (the server still decides). Pure, so it is tested.
 */
import type { Order } from "./orders-api";

type OrderState = Pick<Order,
  "status" | "fulfillmentMethod" | "isBlacklisted" | "paymentStatus" | "paymentRequired" | "upfrontDue" | "remaining">;

export const ORDER_STATUSES = ["New", "Confirmed", "Preparing", "Shipped", "Delivered", "Completed", "Cancelled"] as const;
export type OrderStatus = (typeof ORDER_STATUSES)[number];

export const PAYMENT_STATUSES = ["Unpaid", "PendingVerification", "Paid", "Refunded", "Waived"] as const;
export type PaymentStatus = (typeof PAYMENT_STATUSES)[number];

/** Payment statuses staff may set by hand; refunds and waivers have their own actions. */
export const SETTABLE_PAYMENT_STATUSES = ["Unpaid", "PendingVerification", "Paid"] as const;

/** Money taken at the store or recorded later for any order (the API records every payment as a store payment). */
export const STORE_PAYMENT_METHODS = ["Cash", "InstaPay", "Wallet", "BankTransfer"] as const;
export type StorePaymentMethod = (typeof STORE_PAYMENT_METHODS)[number];

export const ORDER_CHANNELS = ["Online", "Store"] as const;

export function orderStatusKey(status: string): OrderStatus {
  return ORDER_STATUSES.find((value) => value === status) ?? "New";
}

export function paymentStatusKey(status: string): PaymentStatus {
  return PAYMENT_STATUSES.find((value) => value === status) ?? "Unpaid";
}

const isClosed = (order: Pick<Order, "status">) => order.status === "Completed" || order.status === "Cancelled";

/** The steps shown above the order: a pickup order skips Shipped. */
export function orderTrail(order: Pick<Order, "fulfillmentMethod">): OrderStatus[] {
  return order.fulfillmentMethod === "Delivery"
    ? ["New", "Confirmed", "Preparing", "Shipped", "Delivered", "Completed"]
    : ["New", "Confirmed", "Preparing", "Delivered", "Completed"];
}

/** The forward step from the current status (not Completed, which has its own action); null when there is none. */
export function nextStatus(order: Pick<Order, "status" | "fulfillmentMethod">): OrderStatus | null {
  switch (order.status) {
    case "New": return "Confirmed";
    case "Confirmed": return "Preparing";
    case "Preparing": return order.fulfillmentMethod === "Delivery" ? "Shipped" : "Delivered";
    case "Shipped": return "Delivered";
    default: return null;
  }
}

export const canCancel = (order: Pick<Order, "status">) => !isClosed(order);

/**
 * Why the forward step would be refused, or null when it is allowed: a blacklisted customer's order is confirmed only after
 * full prepayment; an order is prepared only after its upfront payment (shipping and deposit).
 */
export function nextStatusBlock(order: OrderState): "blacklist" | "upfront" | null {
  const next = nextStatus(order);
  if (next === "Confirmed" && order.isBlacklisted && order.paymentStatus !== "Paid") return "blacklist";
  if (next === "Preparing" && order.upfrontDue > 0) return "upfront";
  return null;
}

/** Complete is offered from Delivered; it needs Paid when payment is required, else Paid (pickup paid at the store) or an explicit waiver. */
export function completeBlock(order: OrderState): "notDelivered" | "unpaid" | "notWaived" | null {
  if (order.status !== "Delivered") return "notDelivered";
  if (order.paymentRequired && order.paymentStatus !== "Paid") return "unpaid";
  if (!order.paymentRequired && order.paymentStatus !== "Paid" && order.paymentStatus !== "Waived") return "notWaived";
  return null;
}

/** Money can be recorded while something is left to pay on an open order. */
export function canTakePayment(order: OrderState): boolean {
  return !isClosed(order) && order.paymentStatus !== "Waived" && order.paymentStatus !== "Refunded" && order.remaining > 0;
}

export function canWaivePayment(order: OrderState): boolean {
  return !isClosed(order) && order.paymentStatus !== "Paid" && order.paymentStatus !== "Waived";
}

/** The manual status select is hidden once nothing is left to pay (the API refuses "unpaid" on a paid order). */
export function canSetPaymentStatus(order: OrderState): boolean {
  return order.status !== "Cancelled" && order.paymentStatus !== "Waived" && order.paymentStatus !== "Paid" && order.remaining > 0;
}

/** Stock is held only while the order waits to leave the store. */
export const holdsReservation = (order: Pick<Order, "status">) => order.status === "Confirmed" || order.status === "Preparing";

export const hasShipping = (order: Pick<Order, "status" | "fulfillmentMethod">) =>
  order.fulfillmentMethod === "Delivery" && !isClosed(order);

/** The invoice exists once the order is delivered (a later cancel keeps it, voided). */
export const hasInvoice = (order: Pick<Order, "status">) => order.status === "Delivered" || order.status === "Completed";

export type ReservationState =
  | { kind: "none" }
  | { kind: "held"; until: string; hoursLeft: number }
  | { kind: "expired"; at: string };

/** The list's "الحجز" cell: held until (hours left), expired (the admin decides), or nothing. */
export function reservationState(order: Pick<Order, "status" | "reservedUntilUtc" | "reservationExpiredAtUtc">, now: Date): ReservationState {
  if (!holdsReservation(order) || !order.reservedUntilUtc) return { kind: "none" };
  if (order.reservationExpiredAtUtc) return { kind: "expired", at: order.reservationExpiredAtUtc };
  const left = (new Date(order.reservedUntilUtc).getTime() - now.getTime()) / 3_600_000;
  return left <= 0 ? { kind: "expired", at: order.reservedUntilUtc } : { kind: "held", until: order.reservedUntilUtc, hoursLeft: Math.ceil(left) };
}

export interface OrderAddressParts {
  governorate: string | null;
  area: string | null;
  street: string | null;
  building: string | null;
  landmark: string | null;
}

/** The address as one line: governorate › area › street › building, then the landmark when present. */
export function orderAddressText(address: OrderAddressParts | null | undefined, governorate: (code: string) => string): string | null {
  if (!address) return null;
  const first = [address.governorate ? governorate(address.governorate) : null, address.area, address.street, address.building]
    .filter((part): part is string => Boolean(part && part.trim()));
  if (first.length === 0) return address.landmark?.trim() || null;
  return `${first.join(" › ")}${address.landmark?.trim() ? ` · ${address.landmark.trim()}` : ""}`;
}

/** The order history newest first; equal timestamps keep the server's order. */
export function orderTimeline<T extends Pick<Order["events"][number], "createdAtUtc">>(events: readonly T[]): T[] {
  return [...events].sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
}

/** A payment event's amount as a number, or null when the note is not an amount. */
export function eventPaymentAmount(amount: string | null): number | null {
  if (amount === null || amount.trim() === "") return null;
  const value = Number(amount);
  return Number.isFinite(value) ? value : null;
}

/** First line's name and how many more lines the order has. */
export function orderContent(order: Pick<Order, "items">): { first: string; more: number } {
  return { first: order.items[0]?.name ?? "", more: Math.max(0, order.items.length - 1) };
}

const digits = (value: string) => value.replace(/\D/g, "");

/** Search by reference, customer name or phone (any spelling of the digits); channel filter. */
export function filterOrders<T extends Pick<Order, "reference" | "customerName" | "phoneNumber" | "channel">>(
  orders: readonly T[], filters: { q: string; channel: string }
): T[] {
  const q = filters.q.trim().toLowerCase();
  // Phones are stored locally (01…); a typed +20 / 0020 prefix is read the same way.
  const qDigits = digits(q).replace(/^(?:00)?20(?=1)/, "0");
  return orders.filter((order) => {
    if (filters.channel && order.channel !== filters.channel) return false;
    if (!q) return true;
    return order.reference.toLowerCase().includes(q) || order.customerName.toLowerCase().includes(q) ||
      (qDigits.length >= 3 && digits(order.phoneNumber).includes(qDigits));
  });
}

export type OrderEventKey =
  | { key: "created"; channel: string | null }
  | { key: "status"; status: OrderStatus }
  | { key: "payment"; method: string; amount: string | null }
  | { key: "paymentStatus"; status: PaymentStatus }
  | { key: "shipping" | "reservation" | "reservationExpired" | "reservationReleased" }
  | { key: "other"; type: string };

const PAYMENT_STATUS_EVENTS: readonly string[] = ["Unpaid", "PendingVerification", "Paid", "Refunded", "Waived"];

/** The server's event types (`Status:Confirmed`, `Payment:Cash`, …) as a label key for the timeline. */
export function orderEventKey(event: { eventType: string; notes: string | null }): OrderEventKey {
  const [head, tail = ""] = event.eventType.split(":", 2);
  if (head === "OrderCreated") return { key: "created", channel: event.notes };
  if (head === "Status") return { key: "status", status: orderStatusKey(tail) };
  if (head === "Payment") {
    return PAYMENT_STATUS_EVENTS.includes(tail)
      ? { key: "paymentStatus", status: paymentStatusKey(tail) }
      : { key: "payment", method: tail, amount: event.notes };
  }
  if (head === "Shipping") return { key: "shipping" };
  if (head === "Reservation") return { key: "reservation" };
  if (head === "ReservationExpired") return { key: tail === "Released" ? "reservationReleased" : "reservationExpired" };
  return { key: "other", type: event.eventType };
}

/** The payment form's amount: what is left, as text with at most 2 decimals; a typed amount is valid when 0 < amount ≤ left. */
export function paymentAmountValid(text: string, remaining: number): boolean {
  const amount = Number(text);
  return text.trim() !== "" && Number.isFinite(amount) && amount > 0 && Math.round(amount * 100) === amount * 100 && amount <= remaining;
}

export const RESERVATION_MAX_HOURS = 720;

export function reservationHoursValid(text: string): boolean {
  const hours = Number(text);
  return Number.isInteger(hours) && hours >= 1 && hours <= RESERVATION_MAX_HOURS;
}

/** A waiver needs a reason of at least 5 characters. */
export const waiverReasonValid = (reason: string) => reason.trim().length >= 5;

/** The customer's number for `whatsAppUrl`: an Egyptian mobile stored locally (01…) in international form; others unchanged. */
export function customerWhatsAppNumber(phone: string): string {
  const local = digits(phone);
  return local.startsWith("01") && local.length === 11 ? `20${local.slice(1)}` : local;
}
