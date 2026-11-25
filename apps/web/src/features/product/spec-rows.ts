import type { SpecDef } from "../../lib/categories.ts";

export interface SpecRow {
  key: string;
  name: string;
  value: string;
}

/** The words a yes/no spec shows ("نعم" / "لا"), never the stored `yes` / `no`. */
export interface YesNoWords {
  yes: string;
  no: string;
}

const NUMERIC = /^\d+(\.\d+)?$/;
const YES_NO = "YesNo";

function shown(def: SpecDef, value: string, words: YesNoWords): string {
  if (def.type === YES_NO) return value === "yes" ? words.yes : value === "no" ? words.no : value;
  return def.unit && NUMERIC.test(value) ? `${value} ${def.unit}` : value;
}

/** Item specs as table rows: the category's spec order and names first, then any key it does not define (shown as is). */
export function specRows(values: Record<string, string>, defs: SpecDef[], words: YesNoWords): SpecRow[] {
  const known = defs.flatMap((def) => {
    const value = values[def.key]?.trim();
    return value ? [{ key: def.key, name: def.name, value: shown(def, value, words) }] : [];
  });
  const defined = new Set(defs.map((def) => def.key));
  const extra = Object.entries(values).flatMap(([key, value]) => (!defined.has(key) && value.trim() ? [{ key, name: key, value: value.trim() }] : []));
  return [...known, ...extra];
}

/** The longest value a card shows; longer text (a list of ports) stays on the product page. */
const CARD_VALUE_MAX = 16;

/**
 * Up to `max` short facts for a catalog card: the specs buyers filter by first, then the rest, in the category's order.
 * Yes/no specs and plain counts without a unit ("6") are left out, since they mean nothing without their label.
 */
export function cardSpecs(values: Record<string, string>, defs: SpecDef[], max = 3): SpecRow[] {
  const usable = defs.filter((def) => def.type !== YES_NO && !(def.type === "Number" && !def.unit));
  const ranked = [...usable.filter((def) => def.isFilter), ...usable.filter((def) => !def.isFilter)];
  const keys = new Set(ranked.map((def) => def.key));
  return specRows(values, ranked, { yes: "", no: "" })
    .filter((row) => keys.has(row.key) && row.value.length <= CARD_VALUE_MAX)
    .slice(0, max);
}
