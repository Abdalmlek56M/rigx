import type { SpecDef } from "../../lib/categories.ts";

export interface SpecRow {
  key: string;
  name: string;
  value: string;
}

const NUMERIC = /^\d+(\.\d+)?$/;

/** Item specs as table rows: the category's spec order and names first, then any key it does not define (shown as is). */
export function specRows(values: Record<string, string>, defs: SpecDef[]): SpecRow[] {
  const known = defs.flatMap((def) => {
    const value = values[def.key]?.trim();
    return value ? [{ key: def.key, name: def.name, value: def.unit && NUMERIC.test(value) ? `${value} ${def.unit}` : value }] : [];
  });
  const defined = new Set(defs.map((def) => def.key));
  const extra = Object.entries(values).flatMap(([key, value]) => (!defined.has(key) && value.trim() ? [{ key, name: key, value: value.trim() }] : []));
  return [...known, ...extra];
}
