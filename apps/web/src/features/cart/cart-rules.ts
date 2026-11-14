export type Condition = "New" | "Used";
export type PriceOption = "Alone" | "WithBoard" | "InBuild";

export interface CartLine {
  code: string;
  modelName: string;
  condition: Condition;
  warrantyLabel: string | null;
  thumbnailStorageKey: string | null;
  priceOption: PriceOption;
  unitPrice: number;
  quantity: number;
  maxQuantity: number;
}

/** What the API says about an item right now (the fields the cart depends on). */
export interface FreshItem {
  available: number;
  prices: { alone: number; withBoard: number; inBuild: number };
}

const CONDITIONS: readonly string[] = ["New", "Used"];
const PRICE_OPTIONS: readonly string[] = ["Alone", "WithBoard", "InBuild"];

type LineId = Pick<CartLine, "code" | "priceOption">;

export function sameLine(a: LineId, b: LineId): boolean {
  return a.code === b.code && a.priceOption === b.priceOption;
}

/** Used items are one-of-a-kind (always 1); New lots allow 1..maxQuantity. */
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

function isLine(value: unknown): value is CartLine {
  if (typeof value !== "object" || value === null) return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v.code === "string" &&
    typeof v.modelName === "string" &&
    typeof v.condition === "string" && CONDITIONS.includes(v.condition) &&
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
    return parsed.filter(isLine).map((line) => ({ ...line, quantity: clampQuantity(line, line.quantity) }));
  } catch {
    return [];
  }
}
