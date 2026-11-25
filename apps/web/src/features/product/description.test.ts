import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { descriptionParagraphs, metaDescription, productDescription } from "./description.ts";

describe("product description", () => {
  it("takes the page language's text only", () => {
    const item = { description: " وصف ", descriptionEn: "" };
    assert.equal(productDescription(item, "ar"), "وصف");
    assert.equal(productDescription(item, "en"), null);
    assert.equal(productDescription({}, "ar"), null);
  });

  it("splits paragraphs on blank lines", () => {
    assert.deepEqual(descriptionParagraphs("one\ntwo\n\n\nthree\n  \n"), ["one\ntwo", "three"]);
  });

  it("keeps a short meta description on one line", () => {
    assert.equal(metaDescription("بطاقة ممتازة\n\nتعمل بكفاءة"), "بطاقة ممتازة تعمل بكفاءة");
  });

  it("cuts a long one at a whole word", () => {
    assert.equal(metaDescription("alpha beta gamma, delta", 18), "alpha beta gamma…");
    assert.ok(metaDescription("x".repeat(400)).length <= 160);
  });
});
