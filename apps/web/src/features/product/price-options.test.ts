import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { cardPrice, defaultOption, isBuildOnly, offeredOptions, priceChoices, priceOf } from "./price-options.ts";

const prices = { inBuild: 80, withBoard: 90, alone: 100 };
const all = offeredOptions(["Alone", "WithBoard", "InBuild"]);

describe("offered options", () => {
  it("keeps the order the API sent and ignores names it does not know", () => {
    assert.deepEqual(offeredOptions(["WithBoard", "InBuild"]), ["WithBoard", "InBuild"]);
    assert.deepEqual(offeredOptions(["InBuild", "Mystery", "Alone"]), ["InBuild", "Alone"]);
  });
  it("offers everything when the answer carries no list", () => {
    assert.deepEqual(offeredOptions(undefined), ["Alone", "WithBoard", "InBuild"]);
    assert.deepEqual(offeredOptions(null), ["Alone", "WithBoard", "InBuild"]);
  });
  it("starts with the first offered option, Alone when everything is offered", () => {
    assert.equal(defaultOption(all), "Alone");
    assert.equal(defaultOption(["WithBoard", "InBuild"]), "WithBoard");
    assert.equal(defaultOption([]), "Alone");
  });
  it("knows a part that is sold inside a build only", () => {
    assert.equal(isBuildOnly(["InBuild"]), true);
    assert.equal(isBuildOnly(["WithBoard", "InBuild"]), false);
    assert.equal(isBuildOnly(all), false);
    assert.equal(isBuildOnly([]), false);
  });
});

describe("price choices", () => {
  it("lists the three options, cheapest context first", () => {
    assert.deepEqual(priceChoices(prices, all).map((choice) => choice.option), ["InBuild", "WithBoard", "Alone"]);
  });
  it("leaves out the options the category switched off", () => {
    assert.deepEqual(priceChoices(prices, ["WithBoard", "InBuild"]).map((choice) => choice.option), ["InBuild", "WithBoard"]);
  });
  it("offers no choice when one option is left or all the offered prices are the same", () => {
    assert.deepEqual(priceChoices(prices, ["InBuild"]), []);
    assert.deepEqual(priceChoices({ inBuild: 100, withBoard: 100, alone: 100 }, all), []);
    assert.deepEqual(priceChoices({ inBuild: 100, withBoard: 90, alone: 100 }, ["InBuild", "Alone"]), []);
  });
  it("reads the price of an option", () => {
    assert.equal(priceOf(prices, "WithBoard"), 90);
  });
});

describe("card price", () => {
  it("is the price of the default offered option", () => {
    assert.equal(cardPrice({ prices, priceOptions: ["Alone", "WithBoard", "InBuild"] }), 100);
    assert.equal(cardPrice({ prices, priceOptions: ["WithBoard", "InBuild"] }), 90);
    assert.equal(cardPrice({ prices, priceOptions: ["InBuild"] }), 80);
  });
  it("is the standalone price when the answer carries no list", () => {
    assert.equal(cardPrice({ prices }), 100);
  });
});
