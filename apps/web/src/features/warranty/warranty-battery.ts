export type BatteryTone = "good" | "warn" | "low" | "empty";

export interface Battery {
  /** The share of the warranty that is left, 0 to 1. */
  charge: number;
  tone: BatteryTone;
}

const MS_PER_DAY = 86_400_000;
/** Above GOOD_SHARE the bar is green; above LOW_SHARE it is amber; from there down it is red. */
const GOOD_SHARE = 0.5;
const LOW_SHARE = 0.2;

function toneOf(charge: number): BatteryTone {
  if (charge <= 0) return "empty";
  if (charge <= LOW_SHARE) return "low";
  return charge <= GOOD_SHARE ? "warn" : "good";
}

/**
 * The battery of a warranty: how much of its length is left. A running warranty with an end date shows the days left over the
 * whole length; an expired one shows an empty battery; a void one, or one without an end date, shows none.
 */
export function warrantyBattery(status: string, daysLeft: number | null, startsAtUtc: string, endsAtUtc: string | null): Battery | null {
  if (!endsAtUtc) return null;
  if (status === "Expired") return { charge: 0, tone: "empty" };
  if (status !== "Active" || daysLeft === null) return null;
  const total = Math.max(1, Math.round((Date.parse(endsAtUtc) - Date.parse(startsAtUtc)) / MS_PER_DAY));
  const charge = Math.min(1, Math.max(0, daysLeft / total));
  return { charge, tone: toneOf(charge) };
}
