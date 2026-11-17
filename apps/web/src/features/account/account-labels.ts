/** Which message a backend status/type string maps to; labels live in the `account` messages. Pure, so they are tested. */
function known<T extends string>(list: readonly T[], value: string): T | null {
  return list.find((item) => item === value) ?? null;
}

export const REQUEST_TYPES = ["Sell", "TradeIn", "Upgrade", "NotifyMe", "Support", "Contact", "Service"] as const;
export type RequestTypeKey = (typeof REQUEST_TYPES)[number] | "Other";

export const REQUEST_STATUSES = ["New", "Following", "Offered", "Accepted", "Rejected", "Converted", "Closed"] as const;
export type RequestStatusKey = (typeof REQUEST_STATUSES)[number];

/** A request type the account page knows; anything new reads as "Other" rather than raw English. */
export function requestTypeKey(type: string): RequestTypeKey {
  return known(REQUEST_TYPES, type) ?? "Other";
}

/** A request status; an unknown one reads as the first ("received"). */
export function requestStatusKey(status: string): RequestStatusKey {
  return known(REQUEST_STATUSES, status) ?? "New";
}

export const WARRANTY_STATUSES = ["Active", "Expired", "Void"] as const;
export type WarrantyStatusKey = (typeof WARRANTY_STATUSES)[number];

export function warrantyStatusKey(status: string): WarrantyStatusKey | null {
  return known(WARRANTY_STATUSES, status);
}

/** Pill colours of a warranty status (account page and public warranty lookup). */
export const WARRANTY_BADGE: Record<WarrantyStatusKey, string> = {
  Active: "bg-success text-surface",
  Expired: "bg-line text-ink-muted",
  Void: "bg-error text-surface"
};

export type InvoiceKindKey = "Build" | "Parts";

export function invoiceKindKey(kind: string): InvoiceKindKey | null {
  return known(["Build", "Parts"] as const, kind);
}

export type PaymentMethodKey = "CashOnDelivery" | "InstaPay" | "Wallet";

export function paymentMethodKey(method: string): PaymentMethodKey | null {
  return known(["CashOnDelivery", "InstaPay", "Wallet"] as const, method);
}

/** The first `max` names to list on an order card and how many more there are. */
export function itemsPreview(names: readonly string[], max: number): { shown: string[]; more: number } {
  return { shown: names.slice(0, max), more: Math.max(0, names.length - max) };
}

/** The invoice link token for a warranty: matched by invoice number, else by the order it belongs to. */
export function invoiceTokenFor(
  warranty: { invoiceNumber: string | null; orderReference: string },
  invoices: readonly { number: string; orderReference: string; accessToken: string }[]
): string | null {
  const byNumber = warranty.invoiceNumber ? invoices.find((invoice) => invoice.number === warranty.invoiceNumber) : undefined;
  return (byNumber ?? invoices.find((invoice) => invoice.orderReference === warranty.orderReference))?.accessToken ?? null;
}

export interface AccountNavItem {
  href: string;
  key: "orders" | "warranties" | "invoices" | "requests" | "builds" | "addresses" | "profile";
}

export const ACCOUNT_NAV: readonly AccountNavItem[] = [
  { href: "/account", key: "orders" },
  { href: "/account/warranties", key: "warranties" },
  { href: "/account/invoices", key: "invoices" },
  { href: "/account/requests", key: "requests" },
  { href: "/account/builds", key: "builds" },
  { href: "/account/addresses", key: "addresses" },
  { href: "/account/profile", key: "profile" }
];

/** Whether `pathname` (without the locale) belongs to the nav item; the overview also covers the order pages. */
export function isActiveNav(href: string, pathname: string): boolean {
  if (href === "/account") return pathname === "/account" || pathname.startsWith("/account/orders");
  return pathname === href || pathname.startsWith(`${href}/`);
}
