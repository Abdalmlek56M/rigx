import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { forwardKey, fulfillmentKey, methodKey, usageKey } from "./order-labels.ts";

describe("order labels", () => {
  it("knows the payment methods, fulfilment kinds and usages the store uses", () => {
    assert.equal(methodKey("CashOnDelivery"), "CashOnDelivery");
    assert.equal(fulfillmentKey("Pickup"), "Pickup");
    assert.equal(usageKey("Gaming"), "Gaming");
  });

  it("returns null for a value the page has no label for", () => {
    assert.equal(methodKey("Crypto"), null);
    assert.equal(fulfillmentKey(""), null);
    assert.equal(usageKey("Mining"), null);
  });

  it("offers a forward label only for the four forward steps", () => {
    assert.equal(forwardKey("Confirmed"), "Confirmed");
    assert.equal(forwardKey("Delivered"), "Delivered");
    assert.equal(forwardKey("Completed"), null);
    assert.equal(forwardKey(null), null);
  });
});
