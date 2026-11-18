/** Request type/status labels and the actions each state allows. Pure, so it is tested. Messages live under `admin.requests`. */
export const REQUEST_TYPES = ["Sell", "TradeIn", "Upgrade", "NotifyMe", "Support", "Contact", "Service"] as const;
export type RequestTypeKey = (typeof REQUEST_TYPES)[number] | "Other";

export const REQUEST_STATUSES = ["New", "Following", "Offered", "Accepted", "Rejected", "Converted", "Closed"] as const;
export type RequestStatusKey = (typeof REQUEST_STATUSES)[number] | "Other";

function known<T extends string>(list: readonly T[], value: string): T | null {
  return list.find((item) => item === value) ?? null;
}

/** A type the inbox knows; anything new reads as "Other" rather than raw English. */
export function requestTypeKey(type: string): RequestTypeKey {
  return known(REQUEST_TYPES, type) ?? "Other";
}

/** A status the inbox knows; anything new reads as "Other". */
export function requestStatusKey(status: string): RequestStatusKey {
  return known(REQUEST_STATUSES, status) ?? "Other";
}

/** The row title: the item model or code, else the subject, else the first line of the message. */
export function requestTitle(request: { type: string; modelName: string | null; itemCode: string | null; subject?: string | null; message?: string | null; code: string }): string {
  return request.modelName ?? request.itemCode ?? request.subject ?? request.message?.split("\n")[0]?.trim() ?? request.code;
}

const OFFER_TYPES: readonly RequestTypeKey[] = ["Sell", "TradeIn", "Upgrade"];
const OPEN_OFFER_STATUSES: readonly RequestStatusKey[] = ["New", "Following", "Offered", "Rejected"];

/** Sell, trade-in and upgrade requests can take a price offer. */
export function offersAllowed(type: string): boolean {
  return OFFER_TYPES.includes(requestTypeKey(type));
}

/** The offer form is open while the API allows making (or revising) an offer. */
export function canOffer(type: string, status: string): boolean {
  return offersAllowed(type) && OPEN_OFFER_STATUSES.includes(requestStatusKey(status));
}

/** Only a sell or trade-in request with an accepted offer becomes a purchase. */
export function convertAllowed(type: string, status: string): boolean {
  const key = requestTypeKey(type);
  return (key === "Sell" || key === "TradeIn") && requestStatusKey(status) === "Accepted";
}

/** "Follow" starts following a new request (or reopens a closed one). */
export function canFollow(status: string): boolean {
  return requestStatusKey(status) === "New" || requestStatusKey(status) === "Closed";
}

/** Any open request can be closed; converted and already closed ones cannot. */
export function canClose(status: string): boolean {
  return requestStatusKey(status) !== "Converted" && requestStatusKey(status) !== "Closed";
}

/** A decision is only possible while an offer is on the table. */
export function canDecide(status: string): boolean {
  return requestStatusKey(status) === "Offered";
}

/** A notify-me request that matched stock and was not messaged yet. */
export function canMarkBackInStockSent(request: { type: string; availableItemCode: string | null; notifiedAtUtc: string | null }): boolean {
  return requestTypeKey(request.type) === "NotifyMe" && request.availableItemCode !== null && request.notifiedAtUtc === null;
}

export const TRAIL_STEPS = ["New", "Offered", "Accepted", "Converted", "Closed"] as const;
export type TrailStep = (typeof TRAIL_STEPS)[number];

const TRAIL_INDEX: Record<RequestStatusKey, number> = {
  New: 0,
  Following: 0,
  Offered: 1,
  Rejected: 1,
  Accepted: 2,
  Converted: 3,
  Closed: 4,
  Other: 0
};

/** The offer trail for the types that take offers; the index of the current step (null for the other types). */
export function requestTrail(type: string, status: string): { steps: readonly TrailStep[]; current: number } | null {
  if (!offersAllowed(type)) return null;
  return { steps: TRAIL_STEPS, current: TRAIL_INDEX[requestStatusKey(status)] };
}
