import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Faq } from "./faq-api.ts";
import {
  addExcludedCategory,
  addExcludedCode,
  emptyFaqForm,
  faqForm,
  faqScopeKey,
  faqStatusKey,
  faqValid,
  removeValue,
  toFaqInput
} from "./faq-rules.ts";

const faq = (patch: Partial<Faq> = {}): Faq => ({
  id: "1", questionAr: "سؤال", questionEn: "Question", answerAr: "إجابة", answerEn: "Answer", topic: null,
  scope: "General", categoryId: null, itemCode: null, helpPageOnly: false, excludedCategoryIds: [],
  excludedItemCodes: [], youTubeId: null, youTubeEmbedUrl: null, sortOrder: 0, status: "Draft",
  publishedAtUtc: null, createdAtUtc: "2026-11-01T00:00:00Z", updatedAtUtc: "2026-11-01T00:00:00Z",
  rowVersion: 1, ...patch
});

describe("faq labels", () => {
  it("maps known scopes and statuses and falls back", () => {
    assert.equal(faqScopeKey("Category"), "Category");
    assert.equal(faqScopeKey("Store"), "Other");
    assert.equal(faqStatusKey("Published"), "Published");
    assert.equal(faqStatusKey("Deleted"), "Other");
  });
});

describe("faq form", () => {
  it("maps a view to the form and back for the general scope", () => {
    const form = faqForm(faq({ topic: "الشحن", excludedItemCodes: ["RX-0001"], youTubeId: "abc", sortOrder: 4 }));
    assert.deepEqual([form.topic, form.excludedItemCodes, form.youTubeUrl, form.sortOrder], ["الشحن", ["RX-0001"], "abc", "4"]);
    assert.deepEqual(toFaqInput({ ...form, questionAr: " سؤال ", questionEn: " Question ", answerAr: " إجابة ", answerEn: " Answer " }), {
      questionAr: "سؤال", questionEn: "Question", answerAr: "إجابة", answerEn: "Answer", topic: "الشحن", scope: "General",
      categoryId: null, itemCode: null, helpPageOnly: false, excludedCategoryIds: [], excludedItemCodes: ["RX-0001"],
      youTubeUrl: "abc", sortOrder: 4
    });
  });

  it("drops the fields the product scope does not use", () => {
    const input = toFaqInput({
      ...emptyFaqForm(), scope: "Product", itemCode: " rx-0154 ", helpPageOnly: true,
      excludedCategoryIds: ["c1"], excludedItemCodes: ["RX-0001"], categoryId: "c2"
    });
    assert.deepEqual([input.scope, input.itemCode, input.categoryId, input.helpPageOnly, input.excludedCategoryIds, input.excludedItemCodes],
      ["Product", "RX-0154", null, false, [], []]);
  });

  it("requires the four texts and the scope target", () => {
    assert.equal(faqValid(emptyFaqForm()), false);
    assert.equal(faqValid({ ...emptyFaqForm(), questionAr: "q", questionEn: "q", answerAr: "a", answerEn: "a" }), true);
    assert.equal(faqValid({ ...emptyFaqForm(), questionAr: "q", questionEn: "q", answerAr: "a", answerEn: "a", scope: "Category" }), false);
    assert.equal(faqValid({ ...emptyFaqForm(), questionAr: "q", questionEn: "q", answerAr: "a", answerEn: "a", scope: "Category", categoryId: "c1" }), true);
    assert.equal(faqValid({ ...emptyFaqForm(), questionAr: "q", questionEn: "q", answerAr: "a", answerEn: "a", scope: "Product" }), false);
  });

  it("adds unique exclusions and removes them", () => {
    assert.deepEqual(addExcludedCategory(["c1"], "c2"), ["c1", "c2"]);
    assert.deepEqual(addExcludedCategory(["c1"], "c1"), ["c1"]);
    assert.deepEqual(addExcludedCode([], " rx-1 "), ["RX-1"]);
    assert.deepEqual(addExcludedCode(["RX-1"], "rx-1"), ["RX-1"]);
    assert.deepEqual(removeValue(["a", "b", "a"], "a"), ["b"]);
  });
});
