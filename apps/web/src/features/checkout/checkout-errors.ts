import { couponProblemFromFields, type CouponProblemKey } from "../../lib/coupon-problem.ts";

export type BadRequestOutcome =
  /** The coupon was refused at order time: show its sentence, not the server's code. */
  | { status: "coupon"; problem: CouponProblemKey }
  /** The server's field messages (shown as a list). */
  | { status: "invalid"; messages: string[] }
  | { status: "failed" };

/** What a 400 from checkout means, from its field errors. */
export function badRequestOutcome(fieldErrors: Readonly<Record<string, readonly string[]>>): BadRequestOutcome {
  const problem = couponProblemFromFields(fieldErrors);
  if (problem) return { status: "coupon", problem };
  const messages = Object.values(fieldErrors).flat();
  return messages.length > 0 ? { status: "invalid", messages } : { status: "failed" };
}
