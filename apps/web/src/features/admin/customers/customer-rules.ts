/** Small pure helpers for the customer screens. Messages live under `admin.customers`. */
export const CUSTOMER_TABS = ["orders", "warranties", "requests", "notes"] as const;
export type CustomerTab = (typeof CUSTOMER_TABS)[number];

export function customerTab(value: string): CustomerTab {
  return CUSTOMER_TABS.find((tab) => tab === value) ?? "orders";
}

/** "March 2026" in the visitor's language, Cairo time (the customer card's "customer since"). */
export function monthYear(iso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale === "ar" ? "ar-EG-u-nu-latn" : "en-GB", {
    month: "long",
    year: "numeric",
    timeZone: "Africa/Cairo"
  }).format(new Date(iso));
}

/** The governorate name for a code (orders store the ISO code, addresses the name); other text passes through. */
export function governorateName(rates: readonly { code: string; name: string }[], value: string | null): string | null {
  if (!value) return null;
  return rates.find((rate) => rate.code === value)?.name ?? value;
}

/** The blacklist entry that matches this customer's phone or email, if any. */
export function blacklistEntry<T extends { phoneNumber: string | null; email: string | null }>(
  entries: readonly T[],
  phone: string | null,
  email: string | null
): T | null {
  const digits = (phone ?? "").replace(/\D/g, "");
  const address = email?.trim().toLowerCase() ?? "";
  return entries.find((entry) => {
    const entryDigits = (entry.phoneNumber ?? "").replace(/\D/g, "");
    return (digits !== "" && entryDigits === digits) || (address !== "" && entry.email?.trim().toLowerCase() === address);
  }) ?? null;
}

export type OrderChannelKey = "Online" | "Store" | "Other";

export function orderChannelKey(channel: string): OrderChannelKey {
  return channel === "Online" || channel === "Store" ? channel : "Other";
}

export type WarrantyStateKey = "Active" | "Expired" | "Void" | "Other";

export function warrantyStateKey(status: string): WarrantyStateKey {
  return status === "Active" || status === "Expired" || status === "Void" ? status : "Other";
}
