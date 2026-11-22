import { categoryById, type CategoryNode } from "../../lib/categories.ts";
import { localizedName } from "../../lib/localized-name.ts";
import type { SlotInfo } from "./SlotList.tsx";

/** A slot as the API sends it: its name is the category's Arabic name, and `categoryIds` are the categories it covers. */
export interface RawSlot extends SlotInfo {
  categoryIds: readonly string[];
}

/** The slots with each name in the visitor's language: the slot's own category (the first it covers) gives the English name. */
export function localizeSlots(slots: readonly RawSlot[], tree: CategoryNode[], locale: string): SlotInfo[] {
  return slots.map((entry) => {
    const category = entry.categoryIds.map((id) => categoryById(tree, id)).find((node) => node !== undefined);
    return { slot: entry.slot, name: category ? localizedName(category, locale) : entry.name };
  });
}
