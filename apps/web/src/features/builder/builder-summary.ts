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
