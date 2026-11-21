import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { conditionTone, invoiceTone, orderTone, paymentTone, requestTone, warrantyTone } from "./status-badge-rules.ts";

describe("status badge tones", () => {
  it("marks a new order as needing attention and a delivered one as done", () => {
    assert.equal(orderTone("New"), "warning");
    assert.equal(orderTone("Shipped"), "info");
    assert.equal(orderTone("Delivered"), "success");
    assert.equal(orderTone("Cancelled"), "danger");
    assert.equal(orderTone("SomethingNew"), "warning");
  });

  it("keeps an active warranty green, an expired one quiet and a void one red", () => {
    assert.equal(warrantyTone("Active"), "success");
    assert.equal(warrantyTone("Expired"), "neutral");
    assert.equal(warrantyTone("Void"), "danger");
    assert.equal(warrantyTone("Unknown"), "neutral");
  });

  it("maps every request state", () => {
    assert.equal(requestTone("New"), "info");
    assert.equal(requestTone("Offered"), "warning");
    assert.equal(requestTone("Accepted"), "success");
    assert.equal(requestTone("Rejected"), "danger");
    assert.equal(requestTone("Closed"), "neutral");
  });

  it("shows an unpaid order in red and a waived one quietly", () => {
    assert.equal(paymentTone("Unpaid"), "danger");
    assert.equal(paymentTone("Partial"), "warning");
    assert.equal(paymentTone("Paid"), "success");
    assert.equal(paymentTone("Waived"), "neutral");
  });

  it("uses orange only for new condition and graphite for used", () => {
    assert.equal(conditionTone("New"), "warning");
    assert.equal(conditionTone("Used"), "neutral");
  });

  it("marks only a void invoice", () => {
    assert.equal(invoiceTone("Void"), "danger");
    assert.equal(invoiceTone("Issued"), "success");
  });
});
