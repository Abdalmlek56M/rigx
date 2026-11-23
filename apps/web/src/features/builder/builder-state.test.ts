import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  SHARE_NAME_MAX, builderPath, cleanShareName, clearSlot, parseBuilderParams, parsePartsParam, partsOf, partsParam, pickInSlot, quantityCeiling,
  quotePath, removeCode, setQuantity, sharedBuildUrl, slotHasRoom, unitsIn
} from "./builder-state.ts";
import { MAX_PARTS } from "./slot-limits.ts";

describe("parsePartsParam", () => {
  it("reads codes with optional quantities", () => {
    assert.deepEqual(parsePartsParam("RX-1,rx-2:2"), [{ code: "RX-1", quantity: 1 }, { code: "RX-2", quantity: 2 }]);
  });
  it("is empty for missing or empty input", () => {
    assert.deepEqual(parsePartsParam(undefined), []);
    assert.deepEqual(parsePartsParam(""), []);
  });
  it("drops bad codes and repeats, and resets bad quantities to 1", () => {
    assert.deepEqual(parsePartsParam("XX-1,RX-1,RX-1:3,RX-2:9,RX-3:0,RX-4:abc, RX-5 ,RX-"), [
      { code: "RX-1", quantity: 1 },
      { code: "RX-2", quantity: 1 },
      { code: "RX-3", quantity: 1 },
      { code: "RX-4", quantity: 1 },
      { code: "RX-5", quantity: 1 }
    ]);
    assert.equal(parsePartsParam("RX-1:6")[0]?.quantity, 6);
    assert.equal(parsePartsParam("RX-1:7")[0]?.quantity, 1);
    assert.equal(parsePartsParam("RX-1:2.7")[0]?.quantity, 2);
  });
  it("keeps at most MAX_PARTS", () => {
    const value = Array.from({ length: MAX_PARTS + 3 }, (_, index) => `RX-${index + 1}`).join(",");
    assert.equal(parsePartsParam(value).length, MAX_PARTS);
  });
});

describe("partsParam and paths", () => {
  it("round-trips and omits quantity 1", () => {
    const parts = [{ code: "RX-1", quantity: 1 }, { code: "RX-2", quantity: 3 }];
    assert.equal(partsParam(parts), "RX-1,RX-2:3");
    assert.deepEqual(parsePartsParam(partsParam(parts)), parts);
  });
  it("builds builder and quote paths, bare when empty", () => {
    assert.equal(builderPath([]), "/builder");
    assert.equal(quotePath([]), "/builder/quote");
    assert.equal(builderPath([{ code: "RX-1", quantity: 2 }]), "/builder?parts=RX-1%3A2");
    assert.equal(quotePath([{ code: "RX-1", quantity: 1 }]), "/builder/quote?parts=RX-1");
  });
});

describe("parseBuilderParams", () => {
  it("reads parts, a saved token and a build code", () => {
    assert.deepEqual(parseBuilderParams({ parts: "RX-1", saved: " abcdefgh12 ", from: "bld-5" }), {
      parts: [{ code: "RX-1", quantity: 1 }],
      saved: "abcdefgh12",
      from: "BLD-5"
    });
  });
  it("ignores malformed values", () => {
    assert.deepEqual(parseBuilderParams({ saved: "short", from: "RX-1" }), { parts: [], saved: null, from: null });
    assert.deepEqual(parseBuilderParams({}), { parts: [], saved: null, from: null });
    assert.equal(parseBuilderParams({ saved: "has space token" }).saved, null);
  });
  it("takes the first of repeated params", () => {
    assert.equal(parseBuilderParams({ from: ["BLD-1", "BLD-2"] }).from, "BLD-1");
  });
});

describe("cleanShareName", () => {
  it("trims, collapses spaces and returns null when empty", () => {
    assert.equal(cleanShareName("  جهاز   الألعاب  "), "جهاز الألعاب");
    assert.equal(cleanShareName("   "), null);
  });
  it("cuts to the API limit", () => {
    assert.equal(cleanShareName("x".repeat(150))?.length, SHARE_NAME_MAX);
  });
});

describe("sharedBuildUrl", () => {
  it("keeps the language prefix and drops the builder path", () => {
    assert.equal(sharedBuildUrl("https://rigxhardware.com", "/en/builder", "abc12345"), "https://rigxhardware.com/en/b/abc12345");
    assert.equal(sharedBuildUrl("https://rigxhardware.com", "/builder", "abc12345"), "https://rigxhardware.com/b/abc12345");
  });
});

describe("partsOf", () => {
  it("maps an evaluation's parts to inputs", () => {
    const evaluation = { parts: [{ item: { code: "RX-9" }, quantity: 2 }] } as unknown as Parameters<typeof partsOf>[0];
    assert.deepEqual(partsOf(evaluation), [{ code: "RX-9", quantity: 2 }]);
  });
});

describe("choosing in a slot", () => {
  const slotOf = new Map([["RX-1", "CPU"], ["RX-2", "GPU"], ["RX-3", "CPU"], ["RX-4", "SSD"], ["RX-5", "SSD"], ["RX-6", "SSD"], ["RX-7", "SSD"], ["RX-8", "FAN"], ["RX-9", "FAN"], ["RX-10", "RAM"], ["RX-11", "RAM"]]);
  const one = (code: string, quantity = 1) => ({ code, quantity });

  it("replaces what a one-model slot held, with quantity 1", () => {
    assert.deepEqual(pickInSlot([one("RX-1"), one("RX-2")], slotOf, "CPU", "RX-3"), [one("RX-2"), one("RX-3")]);
  });
  it("leaves a chosen one-model part alone, quantity included", () => {
    assert.deepEqual(pickInSlot([one("RX-2", 3)], slotOf, "GPU", "RX-2"), [one("RX-2", 3)]);
  });
  it("keeps the quantity of memory and fans when another model replaces them", () => {
    assert.deepEqual(pickInSlot([one("RX-8", 4)], slotOf, "FAN", "RX-9"), [one("RX-9", 4)]);
    assert.deepEqual(pickInSlot([one("RX-10", 2)], slotOf, "RAM", "RX-11"), [one("RX-11", 2)]);
  });
  it("adds drives one at a time, up to three pieces, and a chosen drive is taken out by choosing it again", () => {
    let parts = pickInSlot([], slotOf, "SSD", "RX-4");
    parts = pickInSlot(parts, slotOf, "SSD", "RX-5");
    parts = pickInSlot(parts, slotOf, "SSD", "RX-6");
    assert.deepEqual(parts, [one("RX-4"), one("RX-5"), one("RX-6")]);
    assert.equal(slotHasRoom(parts, slotOf, "SSD"), false);
    assert.deepEqual(pickInSlot(parts, slotOf, "SSD", "RX-7"), parts);
    assert.deepEqual(pickInSlot(parts, slotOf, "SSD", "RX-5"), [one("RX-4"), one("RX-6")]);
  });
  it("counts the pieces of a slot by quantity", () => {
    const parts = [one("RX-4", 2), one("RX-5"), one("RX-2")];
    assert.equal(unitsIn(parts, slotOf, "SSD"), 3);
    assert.equal(slotHasRoom(parts, slotOf, "SSD"), false);
    assert.equal(slotHasRoom(parts, slotOf, "GPU"), true);
  });
  it("empties a slot of every part in it", () => {
    assert.deepEqual(clearSlot([one("RX-1"), one("RX-4"), one("RX-5"), one("RX-2")], slotOf, "SSD"), [one("RX-1"), one("RX-2")]);
  });
  it("sets a quantity between one and what the slot has room for", () => {
    assert.deepEqual(setQuantity([one("RX-8")], slotOf, "FAN", "RX-8", 6), [one("RX-8", 6)]);
    assert.deepEqual(setQuantity([one("RX-8")], slotOf, "FAN", "RX-8", 9), [one("RX-8", 6)]);
    assert.deepEqual(setQuantity([one("RX-8", 3)], slotOf, "FAN", "RX-8", 0), [one("RX-8")]);
    assert.deepEqual(setQuantity([one("RX-10")], slotOf, "RAM", "RX-10", 5), [one("RX-10", 4)]);
    assert.deepEqual(setQuantity([one("RX-4"), one("RX-5")], slotOf, "SSD", "RX-4", 3), [one("RX-4", 2), one("RX-5")]);
  });
  it("tells the ceiling of one part", () => {
    assert.equal(quantityCeiling([one("RX-8")], slotOf, "FAN", "RX-8"), 6);
    assert.equal(quantityCeiling([one("RX-10")], slotOf, "RAM", "RX-10"), 4);
    assert.equal(quantityCeiling([one("RX-4"), one("RX-5", 2)], slotOf, "SSD", "RX-4"), 1);
  });
  it("removes a code", () => {
    const parts = [one("RX-1"), one("RX-2")];
    assert.deepEqual(removeCode(parts, "RX-1"), [one("RX-2")]);
    assert.deepEqual(removeCode(parts, "RX-99"), parts);
  });
});
