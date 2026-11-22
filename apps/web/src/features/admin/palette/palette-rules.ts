/**
 * The admin command palette (Ctrl+K): what it offers for a set of permissions, how a typed query matches, and how records found
 * by the search become entries. Pure, so it is tested. A person is never offered a page, an action or a record kind their
 * permissions do not open (the API still decides).
 */
import { visibleNav, type AdminAreaKey, type AdminGroupKey } from "../admin-nav.ts";

export type PaletteKind = "action" | "page" | "order" | "customer" | "item" | "purchase";

/** An action that is done by the palette itself instead of opening a page. */
export type PaletteCommand = "toggleTheme" | "toggleDensity";

export interface PaletteEntry {
  id: string;
  kind: PaletteKind;
  label: string;
  /** The quiet second line: the group of a page, the customer of an order. */
  hint?: string;
  /** The sidebar icon of a page. */
  areaKey?: AdminAreaKey;
  /** Which quick action an action entry is (it picks the icon). */
  actionKey?: QuickActionKey;
  href?: string;
  command?: PaletteCommand;
}

export type QuickActionKey = "newSale" | "newPurchase" | "newOrders" | "newRequests" | "toggleTheme" | "toggleDensity";

export interface QuickAction {
  key: QuickActionKey;
  href?: string;
  command?: PaletteCommand;
  /** Any one of these permissions offers the action; empty means everyone who is staff. */
  anyOf: readonly string[];
}

export const QUICK_ACTIONS: readonly QuickAction[] = [
  { key: "newSale", href: "/admin/pos", anyOf: ["Sales.CreatePOS"] },
  { key: "newPurchase", href: "/admin/purchases/new", anyOf: ["Purchasing.CreateOrder"] },
  { key: "newOrders", href: "/admin/orders?status=New", anyOf: ["Sales.ViewOrders"] },
  { key: "newRequests", href: "/admin/requests?status=New", anyOf: ["Requests.View"] },
  { key: "toggleTheme", command: "toggleTheme", anyOf: [] },
  { key: "toggleDensity", command: "toggleDensity", anyOf: [] }
];

export interface StaticLabels {
  area: (key: AdminAreaKey) => string;
  group: (key: AdminGroupKey) => string;
  action: (key: QuickActionKey) => string;
}

/** The quick actions first, then the pages: only what these permissions open. */
export function staticEntries(permissions: readonly string[], labels: StaticLabels): PaletteEntry[] {
  const actions = QUICK_ACTIONS
    .filter((action) => action.anyOf.length === 0 || action.anyOf.some((permission) => permissions.includes(permission)))
    .map((action): PaletteEntry => ({ id: `action:${action.key}`, kind: "action", label: labels.action(action.key), actionKey: action.key, href: action.href, command: action.command }));
  const pages = visibleNav(permissions).flatMap((group) =>
    group.areas.map((area): PaletteEntry => ({ id: `page:${area.key}`, kind: "page", label: labels.area(area.key), hint: labels.group(group.key), areaKey: area.key, href: area.href }))
  );
  return [...actions, ...pages];
}

const TASHKEEL = /[ً-ٰٟـ]/g;

/** A text as the search compares it: no case, no vowel marks, one spelling of alef, yeh and heh, Latin digits. */
export function normalizeSearch(text: string): string {
  return text
    .toLowerCase()
    .replace(TASHKEEL, "")
    .replace(/[أإآٱ]/g, "ا")
    .replace(/ى/g, "ي")
    .replace(/ة/g, "ه")
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 0x0660))
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 0x06f0))
    .trim();
}

/** 0 = the label starts with the word, 1 = a word of the label starts with it, 2 = it is somewhere in the label or hint; null = no match. */
function rank(entry: PaletteEntry, word: string): number | null {
  const label = normalizeSearch(entry.label);
  if (label.startsWith(word)) return 0;
  if (label.split(/\s+/).some((part) => part.startsWith(word))) return 1;
  return label.includes(word) || normalizeSearch(entry.hint ?? "").includes(word) ? 2 : null;
}

/** The entries every word of the query matches, best matches first (stable for equal ranks); an empty query keeps them all in order. */
export function matchEntries(entries: readonly PaletteEntry[], query: string): PaletteEntry[] {
  const words = normalizeSearch(query).split(/\s+/).filter(Boolean);
  if (words.length === 0) return [...entries];
  const scored: { entry: PaletteEntry; score: number; index: number }[] = [];
  entries.forEach((entry, index) => {
    let score = 0;
    for (const word of words) {
      const ranked = rank(entry, word);
      if (ranked === null) return;
      score += ranked;
    }
    scored.push({ entry, score, index });
  });
  return scored.sort((a, b) => a.score - b.score || a.index - b.index).map((item) => item.entry);
}

/** Records are searched from this many characters; shorter text only filters pages and actions. */
export const MIN_RECORD_QUERY = 2;
/** At most this many records of one kind are shown. */
export const RECORDS_PER_KIND = 5;

export type RecordSource = "orders" | "customers" | "items" | "purchases";

const SOURCE_PERMISSION: Record<RecordSource, string> = {
  orders: "Sales.ViewOrders",
  customers: "Customers.View",
  items: "Inventory.View",
  purchases: "Purchasing.View"
};

/** The kinds of record these permissions may search. */
export function recordSources(permissions: readonly string[]): RecordSource[] {
  return (Object.keys(SOURCE_PERMISSION) as RecordSource[]).filter((source) => permissions.includes(SOURCE_PERMISSION[source]));
}

export const canSearchRecords = (query: string): boolean => query.trim().length >= MIN_RECORD_QUERY;

// ---------------------------------------------------------------- records as entries

export const orderEntry = (order: { id: string; reference: string; customerName: string }): PaletteEntry =>
  ({ id: `order:${order.id}`, kind: "order", label: order.reference, hint: order.customerName, href: `/admin/orders?id=${order.id}` });

export const customerEntry = (customer: { id: string; name: string; phone?: string | null }): PaletteEntry =>
  ({ id: `customer:${customer.id}`, kind: "customer", label: customer.name, hint: customer.phone ?? undefined, href: `/admin/customers/${customer.id}` });

export const itemEntry = (item: { id: string; code: string; modelName: string }): PaletteEntry =>
  ({ id: `item:${item.id}`, kind: "item", label: item.code, hint: item.modelName, href: `/admin/inventory/${item.id}` });

export const purchaseEntry = (purchase: { id: string; code: string; seller?: { name: string } | null }): PaletteEntry =>
  ({ id: `purchase:${purchase.id}`, kind: "purchase", label: purchase.code, hint: purchase.seller?.name, href: `/admin/purchases/${purchase.id}` });

/** The row after moving `delta` steps from `current` in a list of `count` rows, wrapping at both ends; -1 means nothing is active yet. */
export function moveActive(current: number, count: number, delta: 1 | -1): number {
  if (count === 0) return -1;
  if (current < 0) return delta === 1 ? 0 : count - 1;
  return (current + delta + count) % count;
}

/** Ctrl+K (⌘K on a Mac) opens and closes the palette, wherever the focus is. */
export function isPaletteShortcut(event: { key: string; ctrlKey: boolean; metaKey: boolean; altKey: boolean; shiftKey: boolean }): boolean {
  return (event.ctrlKey || event.metaKey) && !event.altKey && !event.shiftKey && event.key.toLowerCase() === "k";
}
