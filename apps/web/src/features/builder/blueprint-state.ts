import type { Schemas } from "@rigx/api-client";
import { BLUEPRINT_ORDER } from "./blueprint-shapes.ts";

export type SlotIssue = "none" | "warning" | "conflict";

export interface BlueprintSlot {
  slot: string;
  filled: boolean;
  /** The slot the visitor is choosing for. */
  active: boolean;
  /** The worst problem this slot is part of (either side of a mismatch). */
  issue: SlotIssue;
  /** The chosen model, when filled. */
  model: string | null;
}

/**
 * What each drawn slot looks like: lit when a part is chosen, outlined when the visitor is choosing for it, tinted when it takes part in
 * a compatibility problem. Only slots the store has are drawn, in drawing order (back to front); an unknown slot is left out.
 */
export function blueprintSlots(
  slots: readonly string[],
  parts: readonly Pick<Schemas["BuilderPartView"], "slot" | "item">[],
  issues: readonly Pick<Schemas["BuilderIssue"], "level" | "slot" | "otherSlot">[],
  active: string | null
): BlueprintSlot[] {
  return BLUEPRINT_ORDER.filter((slot) => slots.includes(slot)).map((slot) => {
    const part = parts.find((candidate) => candidate.slot === slot);
    const involved = issues.filter((issue) => issue.slot === slot || issue.otherSlot === slot);
    return {
      slot,
      filled: part !== undefined,
      active: slot === active,
      issue: involved.some((issue) => issue.level === "Conflict") ? "conflict" : involved.length > 0 ? "warning" : "none",
      model: part?.item.modelName ?? null
    };
  });
}
