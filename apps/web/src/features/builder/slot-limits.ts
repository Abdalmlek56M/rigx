/**
 * What a builder slot takes: how many different models and how many pieces in all. The same numbers as `BuilderSlot.LimitOf` in the
 * PcBuilder module (the API refuses anything over them); the screen uses them to offer only what the API will accept.
 */
export interface SlotLimit {
  parts: number;
  units: number;
}

const DEFAULT_LIMIT: SlotLimit = { parts: 1, units: 4 };

const LIMITS: Readonly<Record<string, SlotLimit>> = {
  /** Up to three drives, each a model of its own (the same model twice counts as two pieces). */
  SSD: { parts: 3, units: 3 },
  /** One fan model in a quantity up to six: three in front, two on top, one at the back. */
  FAN: { parts: 1, units: 6 }
};

/** Every slot the builder knows, so the most parts a build can hold is the sum of their limits. */
const KNOWN_SLOTS = ["CPU", "BOARD", "RAM", "GPU", "SSD", "PSU", "CASE", "COOL", "FAN"] as const;

export const slotLimit = (slot: string): SlotLimit => (Object.hasOwn(LIMITS, slot) ? LIMITS[slot]! : DEFAULT_LIMIT);

/** The most parts one build can hold (`BuilderSlot.MaxParts`). */
export const MAX_PARTS = KNOWN_SLOTS.reduce((sum, slot) => sum + slotLimit(slot).parts, 0);

/** The largest quantity any part can carry (`BuilderRules.MaxQuantity`). */
export const MAX_QUANTITY = Math.max(...KNOWN_SLOTS.map((slot) => slotLimit(slot).units));

/** Slots where the visitor sets how many of one model (sticks of memory, fans); the others hold one piece, and storage adds one drive at a time. */
const QUANTITY_SLOTS: ReadonlySet<string> = new Set(["RAM", "FAN"]);

export const takesQuantity = (slot: string): boolean => QUANTITY_SLOTS.has(slot);

/** Whether a slot holds several different models (storage): choosing a chosen one removes it. */
export const takesSeveralParts = (slot: string): boolean => slotLimit(slot).parts > 1;

/** Whether a slot that takes several models (storage) already holds all it can: `held` are the parts in that slot. */
export const slotIsFull = (slot: string, held: readonly { quantity: number }[]): boolean =>
  takesSeveralParts(slot) && held.reduce((sum, part) => sum + part.quantity, 0) >= slotLimit(slot).units;
