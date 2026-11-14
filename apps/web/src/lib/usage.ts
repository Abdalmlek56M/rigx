/** What a build is for (the API's `usage` values); labels live in the `usage` messages. Builds are grouped by usage, never by resolution. */
export const USAGES = ["Gaming", "Editing", "StudyWork", "Streaming"] as const;

export type Usage = (typeof USAGES)[number];

export function parseUsage(value: string | null | undefined): Usage | null {
  return USAGES.find((usage) => usage === value) ?? null;
}
