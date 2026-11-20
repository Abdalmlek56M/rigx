/** The ways a coupon code can be refused (the API's `problem` codes). The sentence for each lives under the `coupon.problems` messages. */
export const COUPON_PROBLEMS = ["NotFound", "Inactive", "NotStarted", "Expired", "BelowMinimum", "UsedUp", "AlreadyUsed"] as const;
export type CouponProblemKey = (typeof COUPON_PROBLEMS)[number] | "Other";

/** A server problem code → the key of its message; an unknown code reads as "Other", never as the raw code. */
export function couponProblemKey(problem: string): CouponProblemKey {
  return COUPON_PROBLEMS.find((known) => known === problem) ?? "Other";
}

/** The coupon problem inside a 400's field errors (`couponCode: ["Expired"]`), or `null` when the failure is about something else. */
export function couponProblemFromFields(fieldErrors: Readonly<Record<string, readonly string[]>>): CouponProblemKey | null {
  const entry = Object.entries(fieldErrors).find(([key]) => key.toLowerCase() === "couponcode");
  const first = entry?.[1][0];
  return first === undefined ? null : couponProblemKey(first);
}
