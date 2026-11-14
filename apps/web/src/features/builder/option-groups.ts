import type { Schemas } from "@rigx/api-client";
import { parseCondition, type Condition } from "../../lib/condition.ts";

type Option = Schemas["BuilderOptionView"];

/** Why a choice cannot be picked: no copy left, or it clashes with the parts already chosen. */
export type ChoiceBlock = "unavailable" | "incompatible";

export interface Choice {
  code: string;
  condition: Condition;
  /** The price the visitor sees for this choice (in-build or alone, as the build will be priced). */
  price: number;
  warrantyLabel: string | null;
  selected: boolean;
  block: ChoiceBlock | null;
  issues: Schemas["BuilderIssue"][];
}

/** One model with its جديد and مستعمل choices; a condition the store does not have for the model is `null`. */
export interface OptionGroup {
  key: string;
  modelName: string;
  thumbnailStorageKey: string | null;
  new: Choice | null;
  used: Choice | null;
}

function toChoice(option: Option, selectedCodes: ReadonlySet<string>, inBuildPrice: boolean): Choice | null {
  const condition = parseCondition(option.item.condition);
  if (!condition) return null;
  const selected = selectedCodes.has(option.item.code);
  const block: ChoiceBlock | null = selected ? null : !option.isAvailable ? "unavailable" : !option.isCompatible ? "incompatible" : null;
  return {
    code: option.item.code,
    condition,
    price: inBuildPrice ? option.item.prices.inBuild : option.item.prices.alone,
    warrantyLabel: option.item.warrantyLabel,
    selected,
    block,
    issues: option.issues
  };
}

/** The more useful of two units of the same model and condition: the chosen one, then pickable ones, then the cheaper. */
function better(candidate: Choice, current: Choice): boolean {
  const rank = (choice: Choice) => (choice.selected ? 0 : choice.block === null ? 1 : choice.block === "incompatible" ? 2 : 3);
  return rank(candidate) !== rank(current) ? rank(candidate) < rank(current) : candidate.price < current.price;
}

/** Groups the API's options (one per stock item) by model, in the order models first appear. */
export function groupOptions(options: readonly Option[], selectedCodes: ReadonlySet<string>, inBuildPrice: boolean): OptionGroup[] {
  const groups = new Map<string, OptionGroup>();
  for (const option of options) {
    const choice = toChoice(option, selectedCodes, inBuildPrice);
    if (!choice) continue;
    const key = option.item.modelName.trim().toLowerCase();
    const group = groups.get(key) ?? { key, modelName: option.item.modelName, thumbnailStorageKey: null, new: null, used: null };
    group.thumbnailStorageKey ??= option.item.thumbnailStorageKey;
    const slot = choice.condition === "New" ? "new" : "used";
    const current = group[slot];
    if (!current || better(choice, current)) group[slot] = choice;
    groups.set(key, group);
  }
  return [...groups.values()];
}
