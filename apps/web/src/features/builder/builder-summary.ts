import type { Schemas } from "@rigx/api-client";
import { itemLine, type CartLine, type PriceOption } from "../cart/cart-rules.ts";
import { parseCondition, type Condition } from "../../lib/condition.ts";

export type Evaluation = Schemas["BuilderEvaluation"];

export const EMPTY_EVALUATION: Evaluation = {
  parts: [],
  missingCodes: [],
  issues: [],
  powerEstimateWatts: null,
  psuCapacityWatts: null,
  usesBuildPrice: false,
  priceOption: "Alone",
  total: 0,
  canOrder: false,
  cartLines: []
};

const CPU = "CPU";
const BOARD = "BOARD";

/**
 * Whether a part picked into `slot` is shown at its in-build price: the API prices every part in-build when the build
 * has a processor and a motherboard (the real prices always come from the check; this only picks which one to show).
 */
export function showsInBuildPrice(filledSlots: Iterable<string>, slot: string): boolean {
  const slots = new Set([...filledSlots, slot]);
  return slots.has(CPU) && slots.has(BOARD);
}

export function conditionCounts(parts: Evaluation["parts"]): Record<Condition, number> {
  const counts: Record<Condition, number> = { New: 0, Used: 0 };
  for (const part of parts) {
    const condition = parseCondition(part.item.condition);
    if (condition) counts[condition] += part.quantity;
  }
  return counts;
}

/** Share of the power supply's output the estimate uses (0.8 = 80%); `null` without both numbers. */
export function powerShare(evaluation: Pick<Evaluation, "powerEstimateWatts" | "psuCapacityWatts">): number | null {
  const { powerEstimateWatts: estimate, psuCapacityWatts: capacity } = evaluation;
  return estimate != null && capacity != null && capacity > 0 ? estimate / capacity : null;
}

/** The share of the power supply's output above which the API warns (`BuilderRules.PowerWarningShare`); the advice below leaves this headroom. */
export const POWER_WARNING_SHARE = 0.8;
const PSU_STEP_WATTS = 50;

/**
 * "ok": the supply has headroom; "near": over the warning share; "over": the estimate is more than the supply gives; "noSupply": no supply
 * chosen (or one without a power figure) yet; "unknown": nothing to estimate from.
 */
export type PowerState = "ok" | "near" | "over" | "noSupply" | "unknown";

export interface PowerAdvice {
  state: PowerState;
  /** Estimated draw and the supply's output in watts, rounded; null when not known. */
  estimate: number | null;
  capacity: number | null;
  /** Share of the supply used (0.8 = 80 %), null without both numbers. */
  share: number | null;
  /** A supply output (a multiple of 50 W) that keeps the estimate under the warning share; null when nothing needs advising. */
  recommended: number | null;
}

/** What the power bar says: the state, the numbers and, when the supply is missing or too small, the output to look for. */
export function powerAdvice(evaluation: Pick<Evaluation, "powerEstimateWatts" | "psuCapacityWatts">): PowerAdvice {
  const share = powerShare(evaluation);
  const estimate = evaluation.powerEstimateWatts == null ? null : Math.round(evaluation.powerEstimateWatts);
  const capacity = evaluation.psuCapacityWatts == null ? null : Math.round(evaluation.psuCapacityWatts);
  const recommended = evaluation.powerEstimateWatts == null ? null : Math.ceil(evaluation.powerEstimateWatts / POWER_WARNING_SHARE / PSU_STEP_WATTS) * PSU_STEP_WATTS;
  if (estimate === null) return { state: "unknown", estimate, capacity, share, recommended: null };
  if (share === null) return { state: "noSupply", estimate, capacity, share, recommended };
  if (share > 1) return { state: "over", estimate, capacity, share, recommended };
  if (share > POWER_WARNING_SHARE) return { state: "near", estimate, capacity, share, recommended };
  return { state: "ok", estimate, capacity, share, recommended: null };
}

/** The width of the power bar's fill in percent: the share of the supply, never past the end of the bar; no supply is an empty bar. */
export function powerFillPercent(advice: Pick<PowerAdvice, "share">): number {
  return advice.share === null ? 0 : Math.min(100, Math.round(advice.share * 100));
}

/** Cart lines for "أضف الكل للسلة": each part as an item priced the way the evaluation priced it. */
export function cartLinesOf(evaluation: Evaluation): CartLine[] {
  const option = evaluation.priceOption as PriceOption;
  return evaluation.parts.flatMap((part) => {
    const condition = parseCondition(part.item.condition);
    return condition ? [itemLine({ ...part.item, condition }, option, part.unitPrice, part.quantity)] : [];
  });
}

/** One line per part for the WhatsApp message: "المعالج: Ryzen 5 5600 (جديد)". */
export function partsSummaryText(
  parts: Evaluation["parts"],
  slotNames: Readonly<Record<string, string>>,
  conditionLabel: (condition: Condition) => string
): string {
  return parts
    .map((part) => {
      const condition = parseCondition(part.item.condition);
      const quantity = part.quantity > 1 ? ` ×${part.quantity}` : "";
      return `${slotNames[part.slot] ?? part.slot}: ${part.item.modelName}${condition ? ` (${conditionLabel(condition)})` : ""}${quantity}`;
    })
    .join("\n");
}
