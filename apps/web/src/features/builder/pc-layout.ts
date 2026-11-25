/**
 * What the case drawing shows of the chosen parts, read from their specs (the keys of the base category spec sets, `BaseCategorySpecs`):
 * the board's size (`form_factor`), its memory slots (`memory_slots`) and Wi-Fi (`wifi`), the sticks in a memory kit (`modules`), the card's
 * fans (`fans`), the fans that come with the case (`fans_included`) and the cooler's kind and radiator (`cooler_type`, `radiator`).
 * A spec the owner has not filled falls back to the most common case.
 */

export type BoardSize = "atx" | "matx" | "itx";

export interface PcLayout {
  board: BoardSize;
  /** Memory slots on the board: 2 or 4. */
  ramSlots: 2 | 4;
  /** Sticks of memory chosen (kits count their sticks), at most the slots. */
  sticks: number;
  /** Fans on the graphics card: 1 to 3. */
  gpuFans: 1 | 2 | 3;
  /** Fans that come with the case: they sit in the first fan places, before any fan chosen on its own. */
  caseFans: number;
  wifi: boolean;
  /** An air tower over the processor, or a liquid cooler (a pump on the processor and a radiator under the top). */
  cooler: "air" | "liquid";
  /** The radiator's 120 mm fans: 1 to 3. */
  radiatorFans: 1 | 2 | 3;
}

export interface LayoutPart {
  slot: string;
  quantity: number;
  item: { specs: Readonly<Record<string, string>> };
}

/** The case has six fan places (three front, two top, one back). */
export const FAN_PLACES = 6;

/** Nothing chosen yet: a full-size board with four slots, a two-fan card and an air cooler, the most common build. */
export const DEFAULT_LAYOUT: PcLayout = { board: "atx", ramSlots: 4, sticks: 0, gpuFans: 2, caseFans: 0, wifi: false, cooler: "air", radiatorFans: 3 };

/** A spec by one of its keys, also when the owner typed the key in another case or with spaces ("Memory Slots"). */
function spec(specs: Readonly<Record<string, string>>, ...keys: string[]): string | null {
  const found = Object.entries(specs).find(([name]) => keys.includes(name.trim().toLowerCase().replace(/\s+/g, "_")));
  const value = found?.[1].trim();
  return value ? value : null;
}

/** The first whole number in a spec ("4", "2 x 8 GB" → 2), or null. */
function count(specs: Readonly<Record<string, string>>, key: string): number | null {
  const match = spec(specs, key)?.match(/\d+/);
  return match ? Number(match[0]) : null;
}

export function boardSize(formFactor: string | null): BoardSize {
  const value = (formFactor ?? "").toLowerCase().replace(/[\s_-]/g, "");
  if (value.includes("itx")) return "itx";
  if (value.includes("micro") || value.includes("matx") || value.includes("µatx") || value.includes("uatx")) return "matx";
  return "atx";
}

const YES = new Set(["yes", "true", "نعم", "1", "y"]);
const LIQUID = /مي[هة]|ماء|مائي|liquid|water|aio/i;

/** Fans on a radiator by its size in mm: 120 is one fan, 240 and 280 two, 360 three; no size is the common 360. */
function radiatorFans(size: string | null): 1 | 2 | 3 {
  const millimetres = Number(size?.match(/\d+/)?.[0] ?? 0);
  if (millimetres === 0) return DEFAULT_LAYOUT.radiatorFans;
  return millimetres >= 360 ? 3 : millimetres >= 240 ? 2 : 1;
}

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

/** The drawing's layout for the chosen parts (`parts` as the builder's check returns them). */
export function pcLayout(parts: readonly LayoutPart[]): PcLayout {
  const first = (slot: string) => parts.find((part) => part.slot === slot)?.item.specs ?? null;
  const board = first("BOARD");
  const size = board ? boardSize(spec(board, "form_factor")) : DEFAULT_LAYOUT.board;
  const slotsSpec = board ? count(board, "memory_slots") : null;
  const ramSlots: 2 | 4 = slotsSpec !== null ? (slotsSpec <= 2 ? 2 : 4) : size === "itx" ? 2 : 4;
  const sticks = parts
    .filter((part) => part.slot === "RAM")
    .reduce((sum, part) => sum + part.quantity * clamp(count(part.item.specs, "modules") ?? 1, 1, 4), 0);
  const gpu = first("GPU");
  const gpuFans = clamp(gpu ? (count(gpu, "fans") ?? DEFAULT_LAYOUT.gpuFans) : DEFAULT_LAYOUT.gpuFans, 1, 3) as 1 | 2 | 3;
  const chassis = first("CASE");
  const caseFans = clamp(chassis ? (count(chassis, "fans_included") ?? 0) : 0, 0, FAN_PLACES);
  const cooler = first("COOL");
  const radiator = cooler ? spec(cooler, "radiator") : null;
  const liquid = cooler !== null && (LIQUID.test(spec(cooler, "cooler_type", "type") ?? "") || radiator !== null);
  return {
    board: size,
    ramSlots,
    sticks: Math.min(sticks, ramSlots),
    gpuFans,
    caseFans,
    wifi: board !== null && YES.has((spec(board, "wifi") ?? "").toLowerCase()),
    cooler: liquid ? "liquid" : "air",
    radiatorFans: radiatorFans(radiator)
  };
}
