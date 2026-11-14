import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  MAX_PARTS, builderPath, parseBuilderParams, parsePartsParam, partsOf, partsParam, quotePath, removeCode, replaceInSlot
} from "./builder-state.ts";

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
    assert.equal(parsePartsParam("RX-1:4")[0]?.quantity, 4);
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

describe("partsOf", () => {
  it("maps an evaluation's parts to inputs", () => {
    const evaluation = { parts: [{ item: { code: "RX-9" }, quantity: 2 }] } as unknown as Parameters<typeof partsOf>[0];
    assert.deepEqual(partsOf(evaluation), [{ code: "RX-9", quantity: 2 }]);
  });
});

describe("replaceInSlot / removeCode", () => {
  const slotOf = new Map([["RX-1", "CPU"], ["RX-2", "GPU"], ["RX-3", "CPU"]]);
  const parts = [{ code: "RX-1", quantity: 1 }, { code: "RX-2", quantity: 1 }];
  it("replaces what the slot held", () => {
    assert.deepEqual(replaceInSlot(parts, slotOf, "CPU", "RX-3"), [{ code: "RX-2", quantity: 1 }, { code: "RX-3", quantity: 1 }]);
  });
  it("empties a slot with null", () => {
    assert.deepEqual(replaceInSlot(parts, slotOf, "CPU", null), [{ code: "RX-2", quantity: 1 }]);
  });
  it("does not duplicate a code already chosen and resets its quantity", () => {
    assert.deepEqual(replaceInSlot([{ code: "RX-2", quantity: 3 }], slotOf, "GPU", "RX-2"), [{ code: "RX-2", quantity: 1 }]);
  });
  it("removes a code", () => {
    assert.deepEqual(removeCode(parts, "RX-1"), [{ code: "RX-2", quantity: 1 }]);
    assert.deepEqual(removeCode(parts, "RX-99"), parts);
  });
});
