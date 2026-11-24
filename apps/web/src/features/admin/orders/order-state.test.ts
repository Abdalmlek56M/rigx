import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canCancel,
  hasInvoice,
  hasShipping,
  holdsReservation,
  orderContent,
  ORDER_STATUSES,
  paymentStatusKey,
  waiverReasonValid
} from "./order-rules.ts";

const statusOnly = (status: string) => ({ status }) as never;
const withFulfillment = (status: string, fulfillmentMethod: string) => ({ status, fulfillmentMethod }) as never;

describe("what an order allows by its status", () => {
  it("lets staff cancel until the order is completed or cancelled", () => {
    const open = ORDER_STATUSES.filter((status) => canCancel(statusOnly(status)));
    assert.deepEqual(open, ["New", "Confirmed", "Preparing", "Shipped", "Delivered"]);
  });

  it("holds stock only while the order waits to leave the store", () => {
    const held = ORDER_STATUSES.filter((status) => holdsReservation(statusOnly(status)));
    assert.deepEqual(held, ["Confirmed", "Preparing"]);
  });

  it("has an invoice once the order is delivered or completed, not before and not when cancelled", () => {
    const invoiced = ORDER_STATUSES.filter((status) => hasInvoice(statusOnly(status)));
    assert.deepEqual(invoiced, ["Delivered", "Completed"]);
  });

  it("shows shipping only for an open delivery order", () => {
    assert.equal(hasShipping(withFulfillment("Preparing", "Delivery")), true);
    assert.equal(hasShipping(withFulfillment("Preparing", "Pickup")), false);
    assert.equal(hasShipping(withFulfillment("Completed", "Delivery")), false);
    assert.equal(hasShipping(withFulfillment("Cancelled", "Delivery")), false);
  });
});

describe("order list helpers", () => {
  it("reads an unknown payment status as unpaid and keeps the known ones", () => {
    assert.equal(paymentStatusKey("Paid"), "Paid");
    assert.equal(paymentStatusKey("Waived"), "Waived");
    assert.equal(paymentStatusKey("something-new"), "Unpaid");
    assert.equal(paymentStatusKey(""), "Unpaid");
  });

  it("names the first line and counts the others", () => {
    assert.deepEqual(orderContent({ items: [{ name: "RTX 3060" }, { name: "Ryzen 5" }, { name: "SSD" }] } as never), { first: "RTX 3060", more: 2 });
    assert.deepEqual(orderContent({ items: [{ name: "RTX 3060" }] } as never), { first: "RTX 3060", more: 0 });
    assert.deepEqual(orderContent({ items: [] } as never), { first: "", more: 0 });
  });

  it("needs a waiver reason of at least 5 characters after trimming", () => {
    assert.equal(waiverReasonValid("خطأ"), false);
    assert.equal(waiverReasonValid("     "), false);
    assert.equal(waiverReasonValid("  abcd  "), false);
    assert.equal(waiverReasonValid("  abcde  "), true);
    assert.equal(waiverReasonValid("عميل قديم"), true);
  });
});
