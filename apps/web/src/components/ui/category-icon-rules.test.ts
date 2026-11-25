import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { categoryIconKey } from "./category-icon-rules.ts";

describe("categoryIconKey", () => {
  it("uses the icon the owner picked", () => {
    assert.equal(categoryIconKey("graphics-cards", "monitor"), "monitor");
  });
  it("guesses from the slug when none is picked or the key is unknown", () => {
    assert.equal(categoryIconKey("graphics-cards", null), "gpu");
    assert.equal(categoryIconKey("cooling", "rocket"), "cooling");
    assert.equal(categoryIconKey("gaming-keyboards"), "keyboard");
    assert.equal(categoryIconKey("power-supplies"), "power");
  });
  it("falls back to the chip for an unknown slug", () => {
    assert.equal(categoryIconKey("misc"), "cpu");
  });
});
