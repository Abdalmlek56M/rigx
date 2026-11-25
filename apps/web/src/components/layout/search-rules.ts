/** The fewest characters worth a live lookup (one letter matches nearly everything). */
export const SUGGEST_MIN_CHARS = 2;
/** How many items the header search suggests; the catalog link shows the rest. */
export const SUGGEST_LIMIT = 5;

/** The text to look up while typing, or null while it is too short. */
export function suggestQuery(text: string): string | null {
  const query = text.trim().replace(/\s+/g, " ");
  return query.length >= SUGGEST_MIN_CHARS ? query : null;
}

/** The highlighted suggestion after an arrow key: wraps around, and -1 (none) when there are no suggestions. */
export function moveHighlight(current: number, step: 1 | -1, count: number): number {
  if (count === 0) return -1;
  if (current < 0) return step === 1 ? 0 : count - 1;
  return (current + step + count) % count;
}
