export const MODERATION_STATUSES = ["Pending", "Approved", "Rejected"] as const;
export type ModerationStatus = (typeof MODERATION_STATUSES)[number];
export type Decision = Exclude<ModerationStatus, "Pending">;

/** A status from the address or from the API (whose generated type allows null for this enum): anything unknown is the queue. */
export function parseStatus(value: string | null | undefined): ModerationStatus {
  return MODERATION_STATUSES.find((status) => status === value) ?? "Pending";
}

/** What the admin can still do with a build in this status: decide a waiting one either way, or change a decided one to the other decision. */
export function decisionsFor(status: ModerationStatus): readonly Decision[] {
  if (status === "Pending") return ["Approved", "Rejected"];
  return status === "Approved" ? ["Rejected"] : ["Approved"];
}
