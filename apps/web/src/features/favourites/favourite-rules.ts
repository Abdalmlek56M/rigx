/** Same limit as the API (`FavouriteRules.Max`): an account or a guest keeps at most this many hearts. */
export const FAVOURITES_MAX = 200;

const SAVED_CODE = /^(?:RX|BLD)-\d+$/;

/** A guest's saved list from localStorage: only item and build codes, no duplicates, never more than the limit; anything else reads as empty. */
export function parseStoredCodes(raw: string | null): string[] {
  if (!raw) return [];
  try {
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    const codes = parsed.filter((value): value is string => typeof value === "string" && SAVED_CODE.test(value));
    return [...new Set(codes)].slice(0, FAVOURITES_MAX);
  } catch {
    return [];
  }
}

/** Adds the code at the front or removes it; adding to a full list changes nothing. */
export function toggleCode(codes: readonly string[], code: string): string[] {
  if (codes.includes(code)) return codes.filter((saved) => saved !== code);
  return codes.length >= FAVOURITES_MAX ? [...codes] : [code, ...codes];
}
