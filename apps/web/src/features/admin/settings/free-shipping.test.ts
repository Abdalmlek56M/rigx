import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { shippingForm, shippingValue, type ShippingSettings } from "./shipping-rules.ts";

const settings = (patch: Partial<ShippingSettings> = {}): ShippingSettings => ({
  courierName: null, pickupEnabled: true, defaultFee: 60, defaultDeliveryTime: null, groups: [], ...patch
});

describe("free shipping setting", () => {
  it("is off with an empty minimum when the server has not sent it", () => {
    const form = shippingForm(settings());
    assert.equal(form.freeShippingEnabled, false);
    assert.equal(form.freeShippingMinimum, "");
  });

  it("shows the saved switch and minimum as text", () => {
    const form = shippingForm(settings({ freeShippingEnabled: true, freeShippingMinimum: 5000 }));
    assert.equal(form.freeShippingEnabled, true);
    assert.equal(form.freeShippingMinimum, "5000");
  });

  it("shows a minimum of zero as an empty box, so zero never looks like a rule", () => {
    assert.equal(shippingForm(settings({ freeShippingEnabled: true, freeShippingMinimum: 0 })).freeShippingMinimum, "");
  });

  it("sends the minimum as a number and an empty box as zero", () => {
    const form = shippingForm(settings({ freeShippingEnabled: true, freeShippingMinimum: 5000 }));
    assert.equal(shippingValue(form).freeShippingMinimum, 5000);
    assert.equal(shippingValue({ ...form, freeShippingMinimum: "  " }).freeShippingMinimum, 0);
    assert.equal(shippingValue({ ...form, freeShippingMinimum: "2500.5" }).freeShippingMinimum, 2500.5);
  });

  it("sends the switch as it is, even when the minimum is empty (the server decides)", () => {
    const form = { ...shippingForm(settings()), freeShippingEnabled: true };
    const value = shippingValue(form);
    assert.equal(value.freeShippingEnabled, true);
    assert.equal(value.freeShippingMinimum, 0);
  });

  it("round-trips the whole form without changing it", () => {
    const original = settings({ freeShippingEnabled: true, freeShippingMinimum: 7500, defaultFee: 80 });
    const back = shippingValue(shippingForm(original));
    assert.equal(back.freeShippingEnabled, true);
    assert.equal(back.freeShippingMinimum, 7500);
    assert.equal(back.defaultFee, 80);
  });
});
