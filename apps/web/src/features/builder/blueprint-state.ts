import type { Schemas } from "@rigx/api-client";
import type { SlotDrawing } from "./blueprint-geometry.ts";
import { BLUEPRINT_ORDER } from "./blueprint-shapes.ts";

export type SlotIssue = "none" | "warning" | "conflict";

export interface BlueprintSlot {
  slot: string;
  filled: boolean;
  /** Pieces held in the slot: three drives are 3, four fans are 4, two sticks of memory are 2. */
  count: number;
  /** The slot the visitor is choosing for. */
  active: boolean;
  /** The worst problem this slot is part of (either side of a mismatch). */
  issue: SlotIssue;
  /** The chosen models (one per part), in the order the parts came. */
  models: string[];
  /** The first chosen model, when filled. */
  model: string | null;
}

/**
 * What each drawn slot looks like: lit when a part is chosen, outlined when the visitor is choosing for it, tinted when it takes part in
 * a compatibility problem. Only slots the store has are drawn, in drawing order (back to front); an unknown slot is left out.
 */
export function blueprintSlots(
  slots: readonly string[],
  parts: readonly Pick<Schemas["BuilderPartView"], "slot" | "item" | "quantity">[],
  issues: readonly Pick<Schemas["BuilderIssue"], "level" | "slot" | "otherSlot">[],
  active: string | null
): BlueprintSlot[] {
  return BLUEPRINT_ORDER.filter((slot) => slots.includes(slot)).map((slot) => {
    const held = parts.filter((candidate) => candidate.slot === slot);
    const involved = issues.filter((issue) => issue.slot === slot || issue.otherSlot === slot);
    const models = held.map((part) => part.item.modelName);
    return {
      slot,
      filled: held.length > 0,
      count: held.reduce((sum, part) => sum + part.quantity, 0),
      active: slot === active,
      issue: involved.some((issue) => issue.level === "Conflict") ? "conflict" : involved.length > 0 ? "warning" : "none",
      models,
      model: models[0] ?? null
    };
  });
}

/** How many of a slot's places are lit: one per piece held, never more than the drawing has places for (a processor held twice is still one). */
export function litPieces(drawing: SlotDrawing | undefined, count: number): number {
  return Math.max(0, Math.min(count, drawing?.pieces.length ?? 0));
}
