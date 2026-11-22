/** Who may open a saved build: only its owner, anyone with the link (the default), or the public gallery once the store approves it. */
export const VISIBILITIES = ["Private", "Link", "Public"] as const;
export type Visibility = (typeof VISIBILITIES)[number];
export const DEFAULT_VISIBILITY: Visibility = "Link";

export type Moderation = "Pending" | "Approved" | "Rejected";

/** The API's visibility text as a known value; anything else is the default (by link). */
export function parseVisibility(value: string | null | undefined): Visibility {
  return VISIBILITIES.find((entry) => entry === value) ?? DEFAULT_VISIBILITY;
}

/** The note under a build: only a public build has a moderation state, and it is the owner's to see. */
export function moderationNote(visibility: Visibility, moderation: Moderation | null | undefined): "pending" | "approved" | "rejected" | null {
  if (visibility !== "Public" || !moderation) return null;
  return moderation === "Approved" ? "approved" : moderation === "Rejected" ? "rejected" : "pending";
}
