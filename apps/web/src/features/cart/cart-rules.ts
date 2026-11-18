import type { Condition } from "../../lib/condition.ts";

export type { Condition };
export type PriceOption = "Alone" | "WithBoard" | "InBuild";
export type LineKind = "Item" | "Build";

export interface CartLine {
  /** `Item` = a stock item (RX-...); `Build` = a ready-made build (BLD-...), always priced InBuild. */
  kind: LineKind;
  code: string;
  modelName: string;
  /** Only items have a condition; builds carry none. */
  condition: Condition | null;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  priceOption: PriceOption;
  unitPrice: number;
  quantity: number;
  maxQuantity: number;
}

/** What the API says about an item or build right now (a build: `available` = copies, all prices = sale price). */
export interface FreshItem {
  available: number;
  prices: { alone: number; withBoard: number; inBuild: number };
}

const CONDITIONS: readonly string[] = ["New", "Used"];
const KINDS: readonly string[] = ["Item", "Build"];
const PRICE_OPTIONS: readonly string[] = ["Alone", "WithBoard", "InBuild"];

type LineId = Pick<CartLine, "code" | "priceOption">;

interface ItemSource {
  code: string;
  modelName: string;
  condition: Condition;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  available: number;
}

/** A cart line for a stock item bought as `priceOption` at `unitPrice`. */
export function itemLine(item: ItemSource, priceOption: PriceOption, unitPrice: number, quantity = 1): CartLine {
  return {
    kind: "Item", code: item.code, modelName: item.modelName, condition: item.condition, warrantyLabel: item.warrantyLabel,
    thumbnailStorageKey: item.thumbnailStorageKey, priceOption, unitPrice, quantity, maxQuantity: item.available
  };
}

interface BuildSource {
  code: string;
  title: string;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  salePrice: number;
  copies: number;
}

/** A cart line for a ready-made build: one price (its sale price), up to its copies. */
export function buildLine(build: BuildSource, quantity = 1): CartLine {
  return {
    kind: "Build", code: build.code, modelName: build.title, condition: null, warrantyLabel: build.warrantyLabel,
    thumbnailStorageKey: build.thumbnailStorageKey, priceOption: "InBuild", unitPrice: build.salePrice, quantity,
    maxQuantity: build.copies
  };
}

export function sameLine(a: LineId, b: LineId): boolean {
  return a.code === b.code && a.priceOption === b.priceOption;
}

/** Used items are one-of-a-kind (always 1); New lots and builds allow 1..maxQuantity (a build's copies). */
export function clampQuantity(line: Pick<CartLine, "condition" | "maxQuantity">, quantity: number): number {
  if (line.condition === "Used") return 1;
  return Math.min(Math.max(1, Math.floor(quantity) || 1), Math.max(1, line.maxQuantity));
}

/** Adds a line, merging with the same code and price option (quantities add up, clamped). */
export function addLine(lines: CartLine[], incoming: CartLine): CartLine[] {
  const existing = lines.find((line) => sameLine(line, incoming));
  if (!existing) return [...lines, { ...incoming, quantity: clampQuantity(incoming, incoming.quantity) }];
  const merged = { ...incoming, quantity: existing.quantity + incoming.quantity };
  return lines.map((line) => (line === existing ? { ...merged, quantity: clampQuantity(merged, merged.quantity) } : line));
}

export function removeLine(lines: CartLine[], code: string, priceOption: PriceOption): CartLine[] {
  return lines.filter((line) => !sameLine(line, { code, priceOption }));
}

export function setLineQuantity(lines: CartLine[], code: string, priceOption: PriceOption, quantity: number): CartLine[] {
  return lines.map((line) => (sameLine(line, { code, priceOption }) ? { ...line, quantity: clampQuantity(line, quantity) } : line));
}

export function itemCount(lines: CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.quantity, 0);
}

export function subtotal(lines: CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.unitPrice * line.quantity, 0);
}

export function priceFor(item: FreshItem, option: PriceOption): number {
  if (option === "Alone") return item.prices.alone;
  return option === "WithBoard" ? item.prices.withBoard : item.prices.inBuild;
}

/** Fresh data for a build, in the shape `reconcile` expects. */
export function buildFreshness(build: { salePrice: number; copies: number }): FreshItem {
  const price = build.salePrice;
  return { available: build.copies, prices: { alone: price, withBoard: price, inBuild: price } };
}

/**
 * Brings lines up to date with the API. `fresh.get(code)`: an item = current data, `null` = gone (404),
 * missing = unknown (request failed), so the line is kept as it is. Sold-out lines are dropped.
 */
export function reconcile(lines: CartLine[], fresh: ReadonlyMap<string, FreshItem | null>): { lines: CartLine[]; removed: number } {
  const kept: CartLine[] = [];
  for (const line of lines) {
    const item = fresh.get(line.code);
    if (item === undefined) {
      kept.push(line);
    } else if (item !== null && item.available > 0) {
      const updated = { ...line, unitPrice: priceFor(item, line.priceOption), maxQuantity: item.available };
      kept.push({ ...updated, quantity: clampQuantity(updated, updated.quantity) });
    }
  }
  return { lines: kept, removed: lines.length - kept.length };
}

/** Lines saved before builds existed have no `kind`: they are items. */
function withKind(value: unknown): unknown {
  return typeof value === "object" && value !== null && !("kind" in value) ? { ...value, kind: "Item" } : value;
}

function isLine(value: unknown): value is CartLine {
  if (typeof value !== "object" || value === null) return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v.kind === "string" && KINDS.includes(v.kind) &&
    typeof v.code === "string" &&
    typeof v.modelName === "string" &&
    (v.kind === "Build" ? v.condition === null : typeof v.condition === "string" && CONDITIONS.includes(v.condition)) &&
    (v.warrantyLabel === null || typeof v.warrantyLabel === "string") &&
    (v.thumbnailStorageKey === null || typeof v.thumbnailStorageKey === "string") &&
    typeof v.priceOption === "string" && PRICE_OPTIONS.includes(v.priceOption) &&
    typeof v.unitPrice === "number" && Number.isFinite(v.unitPrice) &&
    typeof v.quantity === "number" && Number.isFinite(v.quantity) &&
    typeof v.maxQuantity === "number" && Number.isFinite(v.maxQuantity)
  );
}

/** Reads the stored cart; anything malformed yields an empty cart (or drops the bad lines). */
export function parseStoredLines(raw: string | null): CartLine[] {
  if (!raw) return [];
  try {
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed.map(withKind).filter(isLine).map((line) => ({ ...line, quantity: clampQuantity(line, line.quantity) }));
  } catch {
    return [];
  }
}

/**
 * The WhatsApp order message for a cart: the intro, one line per part or build ("- name (code) × qty") and the total.
 * The code stays in the message so the shop knows which piece is meant.
 */
export function cartWhatsAppText(lines: CartLine[], text: { intro: string; total: string }, price: (amount: number) => string): string {
  const rows = lines.map((line) => `- ${line.modelName} (${line.code})${line.quantity > 1 ? ` × ${line.quantity}` : ""}`);
  return [text.intro, ...rows, `${text.total}: ${price(subtotal(lines))}`].join("\n");
}
