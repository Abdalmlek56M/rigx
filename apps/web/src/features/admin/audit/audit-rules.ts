/** Audit screen: view/outcome mappers, Cairo days, the day/kind grouping, the before/after diff and the device name. Pure, so it is tested. */
import type { FilterValues } from "../kit/url-filters";
import { addDays } from "../dashboard/dashboard-rules.ts";
import { cairoDayEndUtc, cairoDayStartUtc } from "../offers/offer-rules.ts";

export const AUDIT_VIEWS = ["changes", "security"] as const;
export type AuditView = (typeof AUDIT_VIEWS)[number];

export function auditView(value: string | undefined): AuditView {
  return AUDIT_VIEWS.find((view) => view === value) ?? "changes";
}

export const AUDIT_OUTCOMES = ["Succeeded", "Failed", "Rejected", "Recorded"] as const;
export type AuditOutcomeKey = (typeof AUDIT_OUTCOMES)[number] | "Other";

export function auditOutcomeKey(outcome: string): AuditOutcomeKey {
  return AUDIT_OUTCOMES.find((known) => known === outcome) ?? "Other";
}

/** Every filter the screen keeps in the URL; the change log reads month → day → entry, the events view takes dates and a page. */
export const AUDIT_FILTERS = {
  view: "changes",
  module: "",
  entity: "",
  action: "",
  day: "",
  from: "",
  to: "",
  page: "1",
  entry: "",
  eventType: "",
  outcome: "",
  search: ""
};

export type AuditValues = FilterValues<typeof AUDIT_FILTERS>;
export type AuditSet = (changes: Partial<AuditValues>) => void;

const DATE = /^\d{4}-\d{2}-\d{2}$/;

/** The events endpoint takes UTC instants: the Cairo start of `from` and the Cairo end of `to` (null for an empty or invalid date). */
export function auditRangeUtc(from: string, to: string): { fromUtc: string | null; toUtc: string | null } {
  return {
    fromUtc: DATE.test(from) ? cairoDayStartUtc(from) : null,
    toUtc: DATE.test(to) ? cairoDayEndUtc(to) : null
  };
}

/** First characters of an id, for a compact table cell (the detail shows the whole id). */
export function shortId(value: string | null): string {
  return value ? value.slice(0, 8) : "";
}

/** A stored value's label from a message group, or the value itself when the group does not know it. */
export function labelOf(names: Readonly<Record<string, string>>, value: string): string {
  return (Object.hasOwn(names, value) ? names[value] : undefined) ?? value;
}

/** The kinds of work the list groups a day's entries by, in the order they show. */
export const AUDIT_GROUPS = ["sales", "stock", "money", "partners", "catalog", "content", "customers", "accounts", "settings", "other"] as const;
export type AuditGroup = (typeof AUDIT_GROUPS)[number];

const GROUP_OF_ENTITY: Readonly<Record<string, AuditGroup>> = {
  invoices: "sales", store_orders: "sales", order_items: "sales", order_item_components: "sales", order_payments: "sales",
  order_returns: "sales", warranties: "sales", warranty_extensions: "sales", offers: "sales", offer_items: "sales", coupons: "sales",
  stock_items: "stock", inventory_units: "stock", assemblies: "stock", assembly_units: "stock", warehouses: "stock",
  stock_imports: "stock", stock_item_old_slugs: "stock", purchases: "stock", purchase_lines: "stock", purchase_receipts: "stock", sellers: "stock",
  money_accounts: "money", money_movements: "money", retail_expenses: "money", recurring_expenses: "money", expense_types: "money",
  products: "catalog", product_images: "catalog", product_specifications: "catalog", product_reviews: "catalog", categories: "catalog", brands: "catalog", brand_categories: "catalog",
  content_articles: "content", content_faqs: "content", content_policies: "content", content_media_assets: "content", store_reviews: "content",
  persons: "customers", customer_addresses: "customers", customer_requests: "customers", blacklist_entries: "customers",
  user_accounts: "accounts", user_roles: "accounts", roles: "accounts", role_permissions: "accounts", account_restrictions: "accounts", external_logins: "accounts",
  store_settings: "settings"
};

/** The kind of work a table's changes belong to; every partner table is "partners", an unknown table "other". */
export function auditGroup(entity: string): AuditGroup {
  if (entity.startsWith("partner")) return "partners";
  return (Object.hasOwn(GROUP_OF_ENTITY, entity) ? GROUP_OF_ENTITY[entity] : undefined) ?? "other";
}

export interface AuditKindGroup<T> {
  group: AuditGroup;
  entries: T[];
}

/** A day's entries (newest first) by kind of work in {@link AUDIT_GROUPS} order; entries keep their order. */
export function groupByKind<T extends { entity: string }>(entries: readonly T[]): AuditKindGroup<T>[] {
  const groups = new Map<AuditGroup, T[]>();
  for (const entry of entries) {
    const group = auditGroup(entry.entity);
    groups.set(group, [...(groups.get(group) ?? []), entry]);
  }
  return AUDIT_GROUPS.filter((group) => groups.has(group)).map((group) => ({ group, entries: groups.get(group)! }));
}

export type DayWhen = "today" | "yesterday" | "earlier";

/** Whether a Cairo date (`YYYY-MM-DD`) is today, yesterday or older. */
export function dayWhen(date: string, today: string): DayWhen {
  return date === today ? "today" : date === addDays(today, -1) ? "yesterday" : "earlier";
}

/** `2026-11` of the Cairo date `2026-11-22`. */
export const monthOfDate = (date: string): string => date.slice(0, 7);

/** The month that starts open: the one holding the day a link points to, else the newest ("" for an empty log). */
export function defaultOpenMonth(months: readonly { month: string }[], day: string): string {
  return months.find((entry) => entry.month === monthOfDate(day))?.month ?? months[0]?.month ?? "";
}

/** The first people of a day card and how many more there are (names are already in the reader's language). */
export function actorsLine(names: readonly string[], limit = 2): { shown: string[]; more: number } {
  return { shown: names.slice(0, limit), more: Math.max(0, names.length - limit) };
}

/** The browser and system a stored user agent names ("Chrome", "Windows"); null parts when it names none. */
export function deviceOf(userAgent: string | null): { browser: string | null; os: string | null } | null {
  if (!userAgent) return null;
  const browser = BROWSERS.find(([pattern]) => pattern.test(userAgent))?.[1] ?? null;
  const os = SYSTEMS.find(([pattern]) => pattern.test(userAgent))?.[1] ?? null;
  return browser || os ? { browser, os } : null;
}

// Order matters: Edge, Opera and Samsung also say "Chrome", and Chrome also says "Safari".
const BROWSERS: readonly [RegExp, string][] = [
  [/Edg(e|A|iOS)?\//, "Edge"], [/OPR\/|Opera/, "Opera"], [/SamsungBrowser\//, "Samsung Internet"],
  [/Firefox\/|FxiOS\//, "Firefox"], [/Chrome\/|CriOS\//, "Chrome"], [/Version\/[\d.]+.*Safari\//, "Safari"]
];
const SYSTEMS: readonly [RegExp, string][] = [
  [/Windows/, "Windows"], [/iPhone/, "iPhone"], [/iPad/, "iPad"], [/Android/, "Android"], [/CrOS/, "ChromeOS"],
  [/Mac OS X|Macintosh/, "Mac"], [/Linux/, "Linux"]
];

/** A stored value as the detail shows it: ids of linked records and masked secrets are never shown as they are. */
export type AuditValue =
  | { kind: "empty" | "yes" | "no" | "masked" }
  | { kind: "linked"; ids: string[] }
  | { kind: "text" | "data"; text: string }
  | { kind: "time"; iso: string };

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const INSTANT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/;
const MASKED = "***";

export function auditValue(value: unknown): AuditValue {
  if (value === null || value === undefined || value === "") return { kind: "empty" };
  if (typeof value === "boolean") return { kind: value ? "yes" : "no" };
  if (typeof value === "number" || typeof value === "bigint") return { kind: "text", text: String(value) };
  if (typeof value === "string") {
    if (value === MASKED) return { kind: "masked" };
    if (GUID.test(value)) return { kind: "linked", ids: [value] };
    return INSTANT.test(value) ? { kind: "time", iso: value } : { kind: "text", text: value };
  }
  if (Array.isArray(value)) {
    if (value.length === 0) return { kind: "empty" };
    if (value.every((item): item is string => typeof item === "string" && GUID.test(item))) return { kind: "linked", ids: value };
    if (value.every((item) => typeof item === "string" || typeof item === "number")) return { kind: "text", text: value.join("، ") };
  }
  return { kind: "data", text: JSON.stringify(value, null, 2) ?? "" };
}

export type DiffStatus = "changed" | "added" | "removed";
const DIFF_ORDER: readonly DiffStatus[] = ["changed", "added", "removed"];

export interface DiffRow {
  field: string;
  status: DiffStatus;
  before: AuditValue;
  after: AuditValue;
}

/** Fields the database keeps for itself (stamps, row versions, derived search keys, token hashes): the log keeps them, the screen does not list them. */
const TECHNICAL_FIELDS: ReadonlySet<string> = new Set([
  "CreatedAtUtc", "UpdatedAtUtc", "RowVersion", "ModelKey", "AddressKey", "NormalizedEmail", "NormalizedUsername",
  "AuthorizationVersion", "RbacRevision", "PhotoTokenHash", "TokenHash"
]);

/** Each field that holds something (and is not a technical one): added (no value before), removed (none after) or changed; changed rows first. */
export function diffRows(changes: readonly { field: string; before?: unknown; after?: unknown }[]): DiffRow[] {
  const rows = changes.flatMap((change): DiffRow[] => {
    if (TECHNICAL_FIELDS.has(change.field)) return [];
    const before = auditValue(change.before);
    const after = auditValue(change.after);
    if (before.kind === "empty" && after.kind === "empty") return [];
    const status = before.kind === "empty" ? "added" : after.kind === "empty" ? "removed" : "changed";
    return [{ field: change.field, status, before, after }];
  });
  return DIFF_ORDER.flatMap((status) => rows.filter((row) => row.status === status));
}

/** Fields that name a record, best first. */
const TITLE_FIELDS = [
  "Name", "FullName", "TitleAr", "Title", "BuildName", "ModelName", "Label", "Number", "ItemCode", "BuildCode", "PurchaseCode",
  "PersonCode", "Code", "Sku", "CustomerName", "SellerName", "RecipientName", "Email", "Username", "PermissionCode", "QuestionAr"
];

/** A readable name for the changed record from its own fields (after, else before); null when the change names none. */
export function entryTitle(changes: readonly { field: string; before?: unknown; after?: unknown }[]): string | null {
  for (const field of TITLE_FIELDS) {
    const change = changes.find((item) => item.field === field);
    for (const value of change ? [change.after, change.before] : []) {
      const shown = auditValue(value);
      if (shown.kind === "text") return shown.text;
    }
  }
  return null;
}
