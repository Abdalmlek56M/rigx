/** Table sort kept as one URL value: `field` ascending, `-field` descending, empty = the API's default order. Pure, so it is tested. */
export type SortDirection = "asc" | "desc";

export interface SortState {
  key: string;
  direction: SortDirection;
}

export function parseSort(value: string | null | undefined): SortState | null {
  const text = value?.trim();
  if (!text || text === "-") return null;
  return text.startsWith("-") ? { key: text.slice(1), direction: "desc" } : { key: text, direction: "asc" };
}

export function formatSort(sort: SortState | null): string {
  if (!sort) return "";
  return sort.direction === "desc" ? `-${sort.key}` : sort.key;
}

/** Clicking a header: a new column sorts ascending, the same column flips, and a third click clears the sort. */
export function nextSort(current: SortState | null, key: string): SortState | null {
  if (!current || current.key !== key) return { key, direction: "asc" };
  return current.direction === "asc" ? { key, direction: "desc" } : null;
}

/** The `aria-sort` of a column header. */
export function ariaSort(current: SortState | null, key: string): "ascending" | "descending" | "none" {
  if (!current || current.key !== key) return "none";
  return current.direction === "asc" ? "ascending" : "descending";
}
