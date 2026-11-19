/** Pure helpers for the per-category spec fields on the stock forms. The server checks the values again (`CategorySpecSchema`). */
import type { SpecDefinition } from "../categories/categories-api";

export type SpecValues = Record<string, string>;

/** Sets or clears one spec; an empty value removes the key (every spec is optional). */
export function setSpec(values: SpecValues, key: string, value: string): SpecValues {
  const next = { ...values };
  if (value.trim() === "") delete next[key];
  else next[key] = value;
  return next;
}

/**
 * The values the category's schema accepts: a key the schema does not define is dropped (the server refuses it).
 * A category with no schema keeps every value, as the server does.
 */
export function specsForSchema(values: SpecValues, schema: readonly SpecDefinition[]): SpecValues {
  if (schema.length === 0) return values;
  const keys = new Set(schema.map((spec) => spec.key));
  return Object.fromEntries(Object.entries(values).filter(([key]) => keys.has(key)));
}

/** The values the schema would drop (keys it does not define), e.g. specs typed freely before the category had fields. */
export function foreignSpecs(values: SpecValues, schema: readonly SpecDefinition[]): [string, string][] {
  const kept = specsForSchema(values, schema);
  return Object.entries(values).filter(([key]) => !(key in kept));
}

/** A field label: the spec's name and its unit, e.g. "الذاكرة (GB)". */
export const specLabel = (spec: Pick<SpecDefinition, "name" | "unit">) => (spec.unit ? `${spec.name} (${spec.unit})` : spec.name);
