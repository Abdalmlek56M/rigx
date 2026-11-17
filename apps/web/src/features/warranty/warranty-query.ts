export const MAX_QUERY_LENGTH = 64;

/** The search text from `?q=`: first value, trimmed and capped; empty when absent. */
export function parseLookupQuery(value: string | string[] | undefined): string {
  const first = Array.isArray(value) ? value[0] : value;
  return (first ?? "").trim().slice(0, MAX_QUERY_LENGTH);
}

/** Path of the lookup page for a search text (no `?q=` for an empty one). */
export function lookupHref(query: string): string {
  const text = query.trim();
  return text ? `/warranty?q=${encodeURIComponent(text)}` : "/warranty";
}

/** Days left are only told for a running warranty. */
export function daysLeftToShow(status: string, daysLeft: number | null): number | null {
  return status === "Active" ? daysLeft : null;
}
