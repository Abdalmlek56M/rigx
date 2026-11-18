/** FAQ form ⇄ API, scope/status labels and the exclusion lists. Pure, so it is tested. */
import type { Faq, FaqInput } from "./faq-api";

export const FAQ_SCOPES = ["General", "Category", "Product"] as const;
export type FaqScopeKey = (typeof FAQ_SCOPES)[number] | "Other";

export function faqScopeKey(scope: string): FaqScopeKey {
  return FAQ_SCOPES.find((known) => known === scope) ?? "Other";
}

export const FAQ_STATUSES = ["Draft", "Review", "Published", "Archived"] as const;
export type FaqStatusKey = (typeof FAQ_STATUSES)[number] | "Other";

export function faqStatusKey(status: string): FaqStatusKey {
  return FAQ_STATUSES.find((known) => known === status) ?? "Other";
}

export interface FaqForm {
  questionAr: string;
  questionEn: string;
  answerAr: string;
  answerEn: string;
  topic: string;
  scope: string;
  categoryId: string;
  itemCode: string;
  helpPageOnly: boolean;
  excludedCategoryIds: string[];
  excludedItemCodes: string[];
  youTubeUrl: string;
  sortOrder: string;
}

export function emptyFaqForm(): FaqForm {
  return {
    questionAr: "", questionEn: "", answerAr: "", answerEn: "", topic: "", scope: "General", categoryId: "",
    itemCode: "", helpPageOnly: false, excludedCategoryIds: [], excludedItemCodes: [], youTubeUrl: "", sortOrder: "0"
  };
}

export function faqForm(faq: Faq): FaqForm {
  return {
    questionAr: faq.questionAr,
    questionEn: faq.questionEn,
    answerAr: faq.answerAr,
    answerEn: faq.answerEn,
    topic: faq.topic ?? "",
    scope: faq.scope,
    categoryId: faq.categoryId ?? "",
    itemCode: faq.itemCode ?? "",
    helpPageOnly: faq.helpPageOnly,
    excludedCategoryIds: [...faq.excludedCategoryIds],
    excludedItemCodes: [...faq.excludedItemCodes],
    youTubeUrl: faq.youTubeId ?? "",
    sortOrder: String(faq.sortOrder)
  };
}

export function faqValid(form: FaqForm): boolean {
  return form.questionAr.trim() !== "" && form.questionEn.trim() !== "" &&
    form.answerAr.trim() !== "" && form.answerEn.trim() !== "" &&
    (form.scope !== "Category" || form.categoryId !== "") &&
    (form.scope !== "Product" || form.itemCode.trim() !== "");
}

/** The request body: only the fields the chosen scope uses, empty optional text as null. */
export function toFaqInput(form: FaqForm): FaqInput {
  const general = form.scope === "General";
  const category = form.scope === "Category";
  const product = form.scope === "Product";
  const sortOrder = Number.parseInt(form.sortOrder, 10);
  return {
    questionAr: form.questionAr.trim(),
    questionEn: form.questionEn.trim(),
    answerAr: form.answerAr.trim(),
    answerEn: form.answerEn.trim(),
    topic: form.topic.trim() === "" ? null : form.topic.trim(),
    scope: form.scope,
    categoryId: category && form.categoryId !== "" ? form.categoryId : null,
    itemCode: product && form.itemCode.trim() !== "" ? form.itemCode.trim().toUpperCase() : null,
    helpPageOnly: general && form.helpPageOnly,
    excludedCategoryIds: general || category ? [...form.excludedCategoryIds] : [],
    excludedItemCodes: general || category ? form.excludedItemCodes.map((code) => code.trim().toUpperCase()).filter(Boolean) : [],
    youTubeUrl: form.youTubeUrl.trim() === "" ? null : form.youTubeUrl.trim(),
    sortOrder: Number.isFinite(sortOrder) ? sortOrder : 0
  };
}

export function addExcludedCategory(ids: readonly string[], id: string): string[] {
  return id !== "" && !ids.includes(id) ? [...ids, id] : [...ids];
}

export function addExcludedCode(codes: readonly string[], code: string): string[] {
  const clean = code.trim().toUpperCase();
  return clean !== "" && !codes.includes(clean) ? [...codes, clean] : [...codes];
}

export function removeValue(list: readonly string[], value: string): string[] {
  return list.filter((entry) => entry !== value);
}
