export type Condition = "New" | "Used";

export const CONDITIONS: readonly Condition[] = ["New", "Used"];

/** Reads a condition from API or URL text; anything else is `null`. */
export function parseCondition(value: string | null | undefined): Condition | null {
  const wanted = value?.trim().toLowerCase();
  return CONDITIONS.find((condition) => condition.toLowerCase() === wanted) ?? null;
}
