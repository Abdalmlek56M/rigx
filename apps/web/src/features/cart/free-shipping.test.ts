import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { freeShippingHint, freeShippingMinimum, freeShippingReached, freeShippingRemaining } from "./free-shipping.ts";

describe("free shipping wording", () => {
  it("is off unless the owner turned it on with a minimum above zero", () => {
    assert.equal(freeShippingMinimum(false, 5000), null);
    assert.equal(freeShippingMinimum(true, 0), null);
    assert.equal(freeShippingMinimum(undefined, undefined), null);
    assert.equal(freeShippingMinimum(true, 5000), 5000);
  });

  it("says what is missing below the minimum and nothing at or above it", () => {
    assert.equal(freeShippingRemaining(5000, 3749.5), 1250.5);
    assert.equal(freeShippingRemaining(5000, 5000), null);
    assert.equal(freeShippingRemaining(5000, 9000), null);
    assert.equal(freeShippingRemaining(null, 100), null);
  });

  it("reaches free shipping exactly at the minimum", () => {
    assert.equal(freeShippingReached(5000, 4999.99), false);
    assert.equal(freeShippingReached(5000, 5000), true);
    assert.equal(freeShippingReached(null, 1e9), false);
  });

  it("hints only when the order is close to the minimum", () => {
    assert.equal(freeShippingHint(5000, 1000), 1000);
    assert.equal(freeShippingHint(5000, 2500), 2500);
    assert.equal(freeShippingHint(5000, 2500.01), null);
    assert.equal(freeShippingHint(5000, null), null);
    assert.equal(freeShippingHint(null, 100), null);
    assert.equal(freeShippingHint(5000, 0), null);
  });
});
