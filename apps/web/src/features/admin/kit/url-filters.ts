/**
 * List filters kept in the URL (so a filtered list can be shared, reloaded and gone back to). Each filter has a default that
 * is left out of the URL. Changing any filter other than the page goes back to page 1. Pure, so it is tested.
 */
export type FilterDefaults = Record<string, string>;

export type FilterValues<T extends FilterDefaults> = { [K in keyof T]: string };

export const PAGE_KEY = "page";

/** The current values: each key from the URL, or its default when missing or empty. */
export function readFilters<T extends FilterDefaults>(params: URLSearchParams, defaults: T): FilterValues<T> {
  const values = { ...defaults } as FilterValues<T>;
  for (const key of Object.keys(defaults) as (keyof T & string)[]) {
    const value = params.get(key)?.trim();
    if (value) values[key] = value;
  }
  return values;
}

/** The query string after applying `changes`: defaults dropped, other params kept, page reset unless it is what changed. */
export function writeFilters<T extends FilterDefaults>(
  params: URLSearchParams,
  defaults: T,
  changes: Partial<FilterValues<T>>
): string {
  const next = new URLSearchParams(params);
  for (const [key, raw] of Object.entries(changes)) {
    const value = (raw ?? "").trim();
    if (!value || value === defaults[key]) next.delete(key);
    else next.set(key, value);
  }
  if (!(PAGE_KEY in changes)) next.delete(PAGE_KEY);
  return next.toString();
}

/** A positive page number from a filter value (anything else is page 1). */
export function pageNumber(value: string | undefined): number {
  const page = Number(value);
  return Number.isInteger(page) && page > 0 ? page : 1;
}
