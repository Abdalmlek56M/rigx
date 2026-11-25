import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { moveHighlight, suggestQuery } from "./search-rules.ts";

describe("suggestQuery", () => {
  it("waits for two characters and tidies spaces", () => {
    assert.equal(suggestQuery(" r "), null);
    assert.equal(suggestQuery("  rtx   4060 "), "rtx 4060");
  });
});

describe("moveHighlight", () => {
  it("starts at either end and wraps around", () => {
    assert.equal(moveHighlight(-1, 1, 3), 0);
    assert.equal(moveHighlight(-1, -1, 3), 2);
    assert.equal(moveHighlight(2, 1, 3), 0);
    assert.equal(moveHighlight(0, -1, 3), 2);
  });
  it("has nothing to highlight in an empty list", () => {
    assert.equal(moveHighlight(1, 1, 0), -1);
  });
});
