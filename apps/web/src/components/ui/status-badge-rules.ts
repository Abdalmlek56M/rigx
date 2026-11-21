/** One place that turns a store status into a badge tone; the labels stay in the messages. Pure, so it is tested. */
export type StatusTone = "neutral" | "success" | "warning" | "danger" | "info";

export type OrderStatus = "New" | "Confirmed" | "Preparing" | "Shipped" | "Delivered" | "Cancelled";
export type WarrantyStatus = "Active" | "Expired" | "Void";
export type RequestStatus = "New" | "Following" | "Offered" | "Accepted" | "Rejected" | "Converted" | "Closed";
export type PaymentStatus = "Unpaid" | "Partial" | "Paid" | "Waived";

const ORDER_TONES: Record<OrderStatus, StatusTone> = {
  New: "warning",
  Confirmed: "info",
  Preparing: "info",
  Shipped: "info",
  Delivered: "success",
  Cancelled: "danger"
};

const WARRANTY_TONES: Record<WarrantyStatus, StatusTone> = {
  Active: "success",
  Expired: "neutral",
  Void: "danger"
};

const REQUEST_TONES: Record<RequestStatus, StatusTone> = {
  New: "info",
  Following: "warning",
  Offered: "warning",
  Accepted: "success",
  Rejected: "danger",
  Converted: "success",
  Closed: "neutral"
};

const PAYMENT_TONES: Record<PaymentStatus, StatusTone> = {
  Unpaid: "danger",
  Partial: "warning",
  Paid: "success",
  Waived: "neutral"
};

/** A status the site does not know reads as the calmer fallback rather than showing a raw English word. */
function toneOf<T extends string>(tones: Record<T, StatusTone>, value: string, fallback: T): StatusTone {
  return (tones as Record<string, StatusTone>)[value] ?? tones[fallback];
}

export function orderTone(status: string): StatusTone {
  return toneOf(ORDER_TONES, status, "New");
}

export function warrantyTone(status: string): StatusTone {
  return toneOf(WARRANTY_TONES, status, "Expired");
}

export function requestTone(status: string): StatusTone {
  return toneOf(REQUEST_TONES, status, "New");
}

export function paymentTone(status: string): StatusTone {
  return toneOf(PAYMENT_TONES, status, "Unpaid");
}

/** "جديد" is the orange attention mark; "مستعمل" is the quiet graphite one. */
export function conditionTone(condition: "New" | "Used"): StatusTone {
  return condition === "New" ? "warning" : "neutral";
}

/** A voided invoice is the only invoice state that needs a mark. */
export function invoiceTone(status: string): StatusTone {
  return status === "Void" ? "danger" : "success";
}
