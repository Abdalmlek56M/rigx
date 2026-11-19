import type { Condition } from "../../lib/condition.ts";

/** Available stock of a category by condition (the store items API `counts`). */
export interface ConditionCounts {
  new: number;
  used: number;
}

/**
 * The "<قسم> جديد / مستعمل" links the mega menu shows for a category. The split only makes sense where used stock exists,
 * so a category with no used stock shows none; otherwise each condition that has stock gets its link.
 */
export function conditionLinks(counts: ConditionCounts | null): Condition[] {
  if (!counts || counts.used <= 0) return [];
  return counts.new > 0 ? ["New", "Used"] : ["Used"];
}
