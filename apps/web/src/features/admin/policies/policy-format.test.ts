import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { parsePolicyBody } from "../../policies/policy-body.ts";
import { applyPolicyFormat } from "./policy-format.ts";

const caret = (text: string, at: number, format: "heading" | "list") => applyPolicyFormat(text, at, at, format);

describe("policy toolbar", () => {
  it("makes the line with the caret a heading and takes it back off", () => {
    const edit = caret("مقدمة", 2, "heading");
    assert.equal(edit.text, "# مقدمة");
    assert.deepEqual(parsePolicyBody(edit.text), [{ type: "heading", text: "مقدمة" }]);
    assert.equal(caret(edit.text, 3, "heading").text, "مقدمة");
  });

  it("keeps a blank line around a heading made in the middle of a paragraph, so the page reads it as a heading", () => {
    const edit = caret("نص أول\nالشحن\nنص ثان", 9, "heading");
    assert.equal(edit.text, "نص أول\n\n# الشحن\n\nنص ثان");
    assert.deepEqual(parsePolicyBody(edit.text).map((block) => block.type), ["paragraph", "heading", "paragraph"]);
    assert.equal(edit.text.slice(edit.selectionStart, edit.selectionEnd), "# الشحن");
  });

  it("turns every selected line into a bullet and back", () => {
    const text = "أ\nب\nج";
    const edit = applyPolicyFormat(text, 0, text.length, "list");
    assert.equal(edit.text, "- أ\n- ب\n- ج");
    assert.deepEqual(parsePolicyBody(edit.text), [{ type: "list", items: ["أ", "ب", "ج"] }]);
    assert.equal(applyPolicyFormat(edit.text, edit.selectionStart, edit.selectionEnd, "list").text, text);
  });

  it("separates a new list from the paragraph above and below it", () => {
    const text = "مقدمة\nالبند الأول\nالبند الثاني\nخاتمة";
    const edit = applyPolicyFormat(text, text.indexOf("البند الأول"), text.indexOf("الثاني"), "list");
    assert.equal(edit.text, "مقدمة\n\n- البند الأول\n- البند الثاني\n\nخاتمة");
    assert.deepEqual(parsePolicyBody(edit.text).map((block) => block.type), ["paragraph", "list", "paragraph"]);
  });

  it("changes a heading into a bullet and a bullet into a heading without leaving both marks", () => {
    assert.equal(caret("# عنوان", 3, "list").text, "- عنوان");
    assert.equal(caret("- بند", 3, "heading").text, "# بند");
  });

  it("leaves blank lines alone and does nothing on an empty line", () => {
    const text = "أ\n\nب";
    assert.equal(applyPolicyFormat(text, 0, text.length, "list").text, "- أ\n\n- ب");
    assert.equal(caret("", 0, "heading").text, "");
    assert.equal(caret("أ\n\nب", 2, "list").text, "أ\n\nب");
  });

  it("ignores the line a selection only touches at its start", () => {
    const text = "أ\nب";
    // The second line is only touched at its start, so it is not part of the list; the list is closed off with a blank line so it stays a list.
    assert.equal(applyPolicyFormat(text, 0, 2, "list").text, "- أ\n\nب");
  });
});
