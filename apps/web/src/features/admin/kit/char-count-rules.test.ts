import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { charCountState, textLength } from "./char-count-rules.ts";

describe("character count", () => {
  it("is fine up to the limit and over after it", () => {
    assert.equal(charCountState(0, 60), "ok");
    assert.equal(charCountState(60, 60), "ok");
    assert.equal(charCountState(61, 60), "over");
  });

  it("does not count the spaces at the ends of the text", () => {
    assert.equal(textLength("  عنوان  "), 5);
    assert.equal(textLength("   "), 0);
  });
});
