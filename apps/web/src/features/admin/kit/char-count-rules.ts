/** A field with a recommended length: the count is read the way a search result is cut, by characters of the trimmed text. */
export type CharCountState = "ok" | "over";

/** The length of what a person typed, as a search engine would count it (the ends of the text do not count). */
export const textLength = (value: string): number => value.trim().length;

export function charCountState(length: number, limit: number): CharCountState {
  return length > limit ? "over" : "ok";
}
