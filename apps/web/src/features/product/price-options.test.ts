import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { priceChoices, priceOf } from "./price-options.ts";

describe("price options", () => {
  it("lists the three options, cheapest context first", () => {
    const choices = priceChoices({ inBuild: 80, withBoard: 90, alone: 100 });
    assert.deepEqual(choices.map((c) => c.option), ["InBuild", "WithBoard", "Alone"]);
  });
  it("offers no choice when all prices are the same", () => {
    assert.deepEqual(priceChoices({ inBuild: 100, withBoard: 100, alone: 100 }), []);
  });
  it("reads the price of an option", () => {
    assert.equal(priceOf({ inBuild: 80, withBoard: 90, alone: 100 }, "WithBoard"), 90);
  });
});
