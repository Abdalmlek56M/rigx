import { categoryById, type CategoryNode } from "../../lib/categories.ts";
import { localizedName } from "../../lib/localized-name.ts";
import type { SlotInfo } from "./SlotList.tsx";

/** A slot as the API sends it: its name is the category's Arabic name, and `categoryIds` are the categories it covers. */
export interface RawSlot extends SlotInfo {
  categoryIds: readonly string[];
}

/** A slot holds one part, so its name is the singular ("معالج", not the category's "المعالجات"): the builder reads "اختر معالج", never "اختر المعالجات". */
const SLOT_NAMES: Readonly<Record<"ar" | "en", Readonly<Record<string, string>>>> = {
  ar: { CPU: "معالج", BOARD: "بوردة", RAM: "رامات", GPU: "كرت شاشة", SSD: "تخزين", PSU: "باور سبلاي", COOL: "تبريد", CASE: "كيسة", FAN: "مروحة" },
  en: { CPU: "Processor", BOARD: "Motherboard", RAM: "Memory", GPU: "Graphics card", SSD: "Storage", PSU: "Power supply", COOL: "Cooling", CASE: "Case", FAN: "Fan" }
};

/** The slots with a singular name in the visitor's language; a slot the store added later falls back to its category's name. */
export function localizeSlots(slots: readonly RawSlot[], tree: CategoryNode[], locale: string): SlotInfo[] {
  const names = SLOT_NAMES[locale === "en" ? "en" : "ar"];
  return slots.map((entry) => {
    const singular = Object.hasOwn(names, entry.slot) ? names[entry.slot] : undefined;
    if (singular) return { slot: entry.slot, name: singular };
    const category = entry.categoryIds.map((id) => categoryById(tree, id)).find((node) => node !== undefined);
    return { slot: entry.slot, name: category ? localizedName(category, locale) : entry.name };
  });
}
