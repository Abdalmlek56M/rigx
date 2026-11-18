/** Small pure helpers shared by the inventory and purchase forms (numbers typed as text, partner choices). Tested next to it. */
import type { Partner } from "../expenses/expenses-api";
import type { ModelSuggestion } from "../purchases/purchases-api";

/** An optional amount typed in a field: empty is `null`, anything that is not a number is `NaN` (so the form can refuse it). */
export function parseAmount(text: string): number | null {
  const clean = text.trim();
  return clean === "" ? null : Number(clean);
}

/** The text a field shows for a stored amount. */
export function amountText(value: number | null | undefined): string {
  return value === null || value === undefined ? "" : String(value);
}

/** True for an empty field or a number that is not negative. */
export function optionalAmountOk(text: string): boolean {
  const value = parseAmount(text);
  return value === null || (Number.isFinite(value) && value >= 0);
}

/** True for a number that is not negative (an empty field is not a number). */
export function amountOk(text: string): boolean {
  const value = parseAmount(text);
  return value !== null && Number.isFinite(value) && value >= 0;
}

/** The partners that can finance a unit: active, paid by the parts they finance (a whole-shop partner never finances units). */
export function financingPartners(partners: readonly Partner[]): Partner[] {
  return partners.filter((partner) => partner.isActive && partner.basis === "FinancedParts");
}

/** The share a unit gives its partner: the unit's own percent, else the partner's default. */
export function partnerShare(partner: Partner, override: number | null): number {
  return override ?? partner.defaultPercent;
}

/** The model fields of a line or a new part: what is typed, the catalog product it was matched to, and its category. */
interface ModelFields {
  modelName: string;
  productId: string;
  categoryId: string;
}

/** A typed model name is no longer the suggestion that was picked: it is free text again. */
export const editModel = <T extends ModelFields>(fields: T, modelName: string): T => ({ ...fields, modelName, productId: "" });

/** Picking a suggestion fills the model and its product (and the category when there is none); it is never forced. */
export function applySuggestion<T extends ModelFields>(fields: T, suggestion: ModelSuggestion): T {
  return {
    ...fields,
    modelName: suggestion.modelName,
    productId: suggestion.productId ?? "",
    categoryId: fields.categoryId || suggestion.categoryId || ""
  };
}
