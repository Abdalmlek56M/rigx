import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { MAX_PARTS, MAX_QUANTITY, slotIsFull, slotLimit, takesQuantity, takesSeveralParts } from "./slot-limits.ts";

describe("slot limits (the API's BuilderSlot.LimitOf)", () => {
  it("takes up to three drives and up to six fans of one model, one model and up to four pieces elsewhere", () => {
    assert.deepEqual(slotLimit("SSD"), { parts: 3, units: 3 });
    assert.deepEqual(slotLimit("FAN"), { parts: 1, units: 6 });
    assert.deepEqual(slotLimit("RAM"), { parts: 1, units: 4 });
    assert.deepEqual(slotLimit("SOMETHING"), { parts: 1, units: 4 });
    assert.deepEqual(slotLimit("constructor"), { parts: 1, units: 4 });
  });
  it("adds the parts of every slot up to eleven, with a largest quantity of six", () => {
    assert.equal(MAX_PARTS, 11);
    assert.equal(MAX_QUANTITY, 6);
  });
  it("lets the visitor set a quantity for memory and fans only, and add several parts to storage only", () => {
    assert.deepEqual(["RAM", "FAN", "CPU", "SSD"].map(takesQuantity), [true, true, false, false]);
    assert.deepEqual(["SSD", "FAN", "RAM"].map(takesSeveralParts), [true, false, false]);
  });

  it("is full when storage holds three pieces, and never for a slot that swaps its one model", () => {
    assert.equal(slotIsFull("SSD", [{ quantity: 1 }, { quantity: 1 }]), false);
    assert.equal(slotIsFull("SSD", [{ quantity: 2 }, { quantity: 1 }]), true);
    assert.equal(slotIsFull("FAN", [{ quantity: 6 }]), false);
  });
});
