import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { slideDirection, stepBy } from "./recommend-steps.ts";

describe("stepBy", () => {
  it("moves one step and stops at both ends", () => {
    assert.equal(stepBy("usage", 1), "budget");
    assert.equal(stepBy("budget", 1), "send");
    assert.equal(stepBy("send", 1), "send");
    assert.equal(stepBy("send", -1), "budget");
    assert.equal(stepBy("usage", -1), "usage");
  });
});

describe("slideDirection", () => {
  it("slides forward to a later step and back to an earlier one", () => {
    assert.equal(slideDirection("usage", "budget"), "forward");
    assert.equal(slideDirection("usage", "send"), "forward");
    assert.equal(slideDirection("send", "usage"), "back");
    assert.equal(slideDirection("budget", "usage"), "back");
  });
});
