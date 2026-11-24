import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { headingId, parsePolicyBody, policyApiKind, policyDescription, policyHeadings } from "./policy-body.ts";

describe("policyApiKind", () => {
  it("maps the URL slugs to API kinds", () => {
    assert.equal(policyApiKind("shipping"), "Shipping");
    assert.equal(policyApiKind("terms"), "Terms");
    assert.equal(policyApiKind("privacy"), "Privacy");
    assert.equal(policyApiKind("cookies"), null);
    assert.equal(policyApiKind("Shipping"), null);
  });
});

describe("parsePolicyBody", () => {
  it("splits paragraphs on blank lines and keeps line breaks", () => {
    assert.deepEqual(parsePolicyBody("one\r\ntwo\n\nthree"), [
      { type: "paragraph", lines: ["one", "two"] },
      { type: "paragraph", lines: ["three"] }
    ]);
  });
  it("reads headings and bullet lists", () => {
    assert.deepEqual(parsePolicyBody("## Title\n\n- a\n- b\n\n# Next\ntext"), [
      { type: "heading", text: "Title" },
      { type: "list", items: ["a", "b"] },
      { type: "heading", text: "Next" },
      { type: "paragraph", lines: ["text"] }
    ]);
  });
  it("keeps HTML as plain text", () => {
    assert.deepEqual(parsePolicyBody("<script>x</script>"), [{ type: "paragraph", lines: ["<script>x</script>"] }]);
  });
  it("ignores empty bodies", () => {
    assert.deepEqual(parsePolicyBody(" \n\n "), []);
  });
});

describe("policyHeadings", () => {
  it("lists the headings with stable ids in reading order", () => {
    assert.deepEqual(policyHeadings("## الشحن\n\nنص\n\n## الاسترجاع\n\n- بند"), [
      { id: headingId(0), text: "الشحن" },
      { id: headingId(1), text: "الاسترجاع" }
    ]);
  });
  it("has no headings for a plain paragraph", () => {
    assert.deepEqual(policyHeadings("نص فقط"), []);
  });
});

describe("policyDescription", () => {
  it("uses the opening paragraph", () => {
    assert.equal(policyDescription("نوفّر الشحن لكل المحافظات.\n\n# المدة\n- من 3 إلى 5 أيام."), "نوفّر الشحن لكل المحافظات.");
  });

  it("falls back to the headings when there is no opening paragraph", () => {
    assert.equal(policyDescription("# المدة\n- 14 يومًا.\n\n# الشروط\n- بحالته الأصلية."), "المدة، الشروط");
  });

  it("cuts a long opening at a word and adds an ellipsis", () => {
    const text = policyDescription(Array.from({ length: 60 }, () => "كلمة").join(" "));
    assert.ok(text.length <= 161);
    assert.ok(text.endsWith("…"));
  });

  it("is empty for an empty body", () => {
    assert.equal(policyDescription(""), "");
  });
});
