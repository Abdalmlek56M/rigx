import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { CartLine } from "../cart/cart-rules.ts";
import { EMPTY_FORM, toCheckoutRequest, toOrderLines, validateCheckout, type CheckoutFormState } from "./checkout-request.ts";

const item = (overrides: Partial<CartLine> = {}): CartLine => ({
  kind: "Item", code: "RX-0001", modelName: "Ryzen 5 5600", condition: "New", warrantyLabel: null, thumbnailStorageKey: null,
  priceOption: "WithBoard", unitPrice: 100, quantity: 2, maxQuantity: 5, ...overrides
});
const build = (overrides: Partial<CartLine> = {}): CartLine => ({
  kind: "Build", code: "BLD-01", modelName: "Gaming PC", condition: null, warrantyLabel: null, thumbnailStorageKey: null,
  priceOption: "InBuild", unitPrice: 20000, quantity: 3, maxQuantity: 3, ...overrides
});

const valid = (overrides: Partial<CheckoutFormState> = {}): CheckoutFormState => ({
  ...EMPTY_FORM, name: "  Omar ", phone: "0128 918 6840", governorate: "ALX", area: " Agami ", street: "Main st",
  paymentMethod: "CashOnDelivery", termsAccepted: true, ...overrides
});

describe("toOrderLines", () => {
  it("keeps item quantity and price option", () => {
    assert.deepEqual(toOrderLines([item()]), [
      { code: "RX-0001", stockItemId: null, assemblyId: null, salePrice: null, parts: null, quantity: 2, priceOption: "WithBoard" }
    ]);
  });
  it("splits a build into one line per copy", () => {
    const lines = toOrderLines([build()]);
    assert.equal(lines.length, 3);
    assert.ok(lines.every((line) => line.code === "BLD-01" && line.quantity === 1));
  });
});

describe("toCheckoutRequest", () => {
  it("trims, normalises the phone and nulls empty optionals", () => {
    const request = toCheckoutRequest(valid(), [item()]);
    assert.deepEqual(request.customer, { name: "Omar", phone: "01289186840", phoneHasWhatsApp: true, email: null });
    assert.deepEqual(request.address, { governorate: "ALX", area: "Agami", street: "Main st", building: null, landmark: null });
    assert.equal(request.notes, null);
    assert.equal(request.termsAccepted, true);
  });
  it("sends the applied coupon code, or null", () => {
    assert.equal(toCheckoutRequest(valid(), [item()]).couponCode, null);
    assert.equal(toCheckoutRequest(valid({ couponCode: "AHMED10" }), [item()]).couponCode, "AHMED10");
  });
  it("sends no address for pickup", () => {
    assert.equal(toCheckoutRequest(valid({ fulfillment: "Pickup" }), [item()]).address, null);
  });
  it("keeps the usage note only for Other", () => {
    assert.equal(toCheckoutRequest(valid({ usage: "Other", usageNote: " x " }), []).usageNote, "x");
    assert.equal(toCheckoutRequest(valid({ usage: "Gaming", usageNote: "x" }), []).usageNote, null);
  });
});

describe("validateCheckout", () => {
  it("accepts a complete form", () => {
    assert.deepEqual(validateCheckout(valid()), []);
  });
  it("flags everything missing on an empty form", () => {
    assert.deepEqual(validateCheckout(EMPTY_FORM), ["name", "phone", "governorate", "area", "street", "paymentMethod", "terms"]);
  });
  it("does not ask pickup for an address", () => {
    assert.deepEqual(validateCheckout(valid({ fulfillment: "Pickup", governorate: "", area: "", street: "" })), []);
  });
  it("rejects a non-Egyptian mobile and accepts spaces", () => {
    assert.deepEqual(validateCheckout(valid({ phone: "0138 918 6840" })), ["phone"]);
    assert.deepEqual(validateCheckout(valid({ phone: "011 1111 1111" })), []);
  });
  it("needs a note when the usage is Other", () => {
    assert.deepEqual(validateCheckout(valid({ usage: "Other", usageNote: " " })), ["usageNote"]);
  });
});
