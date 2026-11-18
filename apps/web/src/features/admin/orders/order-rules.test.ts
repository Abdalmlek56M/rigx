import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canTakePayment,
  canWaivePayment,
  completeBlock,
  customerWhatsAppNumber,
  filterOrders,
  nextStatus,
  nextStatusBlock,
  orderEventKey,
  orderTrail,
  paymentAmountValid,
  reservationHoursValid,
  reservationState
} from "./order-rules.ts";

const order = (patch: Record<string, unknown> = {}) => ({
  status: "New", fulfillmentMethod: "Delivery", isBlacklisted: false, paymentStatus: "Unpaid", paymentRequired: true,
  upfrontDue: 0, remaining: 1000, reservedUntilUtc: null as string | null, reservationExpiredAtUtc: null as string | null,
  ...patch
});

describe("order workflow", () => {
  it("walks delivery through Shipped and pickup straight to Delivered", () => {
    assert.equal(nextStatus(order({ status: "Preparing" })), "Shipped");
    assert.equal(nextStatus(order({ status: "Preparing", fulfillmentMethod: "Pickup" })), "Delivered");
    assert.equal(nextStatus(order({ status: "Delivered" })), null);
    assert.deepEqual(orderTrail(order({ fulfillmentMethod: "Pickup" })), ["New", "Confirmed", "Preparing", "Delivered", "Completed"]);
  });

  it("blocks confirming a blacklisted customer until fully paid", () => {
    assert.equal(nextStatusBlock(order({ isBlacklisted: true })), "blacklist");
    assert.equal(nextStatusBlock(order({ isBlacklisted: true, paymentStatus: "Paid" })), null);
  });

  it("blocks preparing until the upfront payment is in", () => {
    assert.equal(nextStatusBlock(order({ status: "Confirmed", upfrontDue: 150 })), "upfront");
    assert.equal(nextStatusBlock(order({ status: "Confirmed" })), null);
  });

  it("completes a delivered order only when paid, or waived when payment is not required", () => {
    assert.equal(completeBlock(order({ status: "Shipped" })), "notDelivered");
    assert.equal(completeBlock(order({ status: "Delivered" })), "unpaid");
    assert.equal(completeBlock(order({ status: "Delivered", paymentStatus: "Paid" })), null);
    assert.equal(completeBlock(order({ status: "Delivered", paymentRequired: false, paymentStatus: "Unpaid" })), "notWaived");
    assert.equal(completeBlock(order({ status: "Delivered", paymentRequired: false, paymentStatus: "Paid" })), null);
    assert.equal(completeBlock(order({ status: "Delivered", paymentRequired: false, paymentStatus: "Waived" })), null);
  });
});

describe("payments", () => {
  it("takes money only on an open order with something left", () => {
    assert.equal(canTakePayment(order()), true);
    assert.equal(canTakePayment(order({ remaining: 0 })), false);
    assert.equal(canTakePayment(order({ status: "Cancelled" })), false);
    assert.equal(canTakePayment(order({ paymentStatus: "Waived" })), false);
  });

  it("waives only an unpaid open order", () => {
    assert.equal(canWaivePayment(order()), true);
    assert.equal(canWaivePayment(order({ paymentStatus: "Paid" })), false);
    assert.equal(canWaivePayment(order({ status: "Completed" })), false);
  });

  it("accepts a positive amount with at most 2 decimals up to what is left", () => {
    assert.equal(paymentAmountValid("250.5", 1000), true);
    assert.equal(paymentAmountValid("1000", 1000), true);
    assert.equal(paymentAmountValid("1000.01", 1000), false);
    assert.equal(paymentAmountValid("0", 1000), false);
    assert.equal(paymentAmountValid("1.005", 1000), false);
    assert.equal(paymentAmountValid("", 1000), false);
  });
});

describe("reservation", () => {
  const now = new Date("2026-11-18T10:00:00Z");

  it("shows hours left while held, expired after, nothing outside Confirmed/Preparing", () => {
    assert.deepEqual(reservationState(order({ status: "Confirmed", reservedUntilUtc: "2026-11-18T20:00:00Z" }), now),
      { kind: "held", until: "2026-11-18T20:00:00Z", hoursLeft: 10 });
    assert.equal(reservationState(order({ status: "Confirmed", reservedUntilUtc: "2026-11-18T09:00:00Z" }), now).kind, "expired");
    assert.equal(reservationState(order({ status: "Preparing", reservedUntilUtc: "2026-11-19T09:00:00Z", reservationExpiredAtUtc: "2026-11-18T09:00:00Z" }), now).kind, "expired");
    assert.equal(reservationState(order({ status: "Shipped", reservedUntilUtc: "2026-11-19T09:00:00Z" }), now).kind, "none");
  });

  it("accepts whole hours from 1 to 720", () => {
    assert.equal(reservationHoursValid("24"), true);
    assert.equal(reservationHoursValid("0"), false);
    assert.equal(reservationHoursValid("721"), false);
    assert.equal(reservationHoursValid("1.5"), false);
  });
});

describe("list and timeline", () => {
  const rows = [
    { reference: "RX-1044", customerName: "Ahmed", phoneNumber: "01012345772", channel: "Online" },
    { reference: "RX-1043", customerName: "منى", phoneNumber: "01198765310", channel: "Store" }
  ];

  it("searches by reference, name or phone digits and filters the channel", () => {
    assert.deepEqual(filterOrders(rows, { q: "1044", channel: "" }).map((row) => row.reference), ["RX-1044"]);
    assert.deepEqual(filterOrders(rows, { q: "منى", channel: "" }).map((row) => row.reference), ["RX-1043"]);
    assert.deepEqual(filterOrders(rows, { q: "+20 101 234", channel: "" }).map((row) => row.reference), ["RX-1044"]);
    assert.deepEqual(filterOrders(rows, { q: "", channel: "Store" }).map((row) => row.reference), ["RX-1043"]);
  });

  it("reads the server's event types", () => {
    assert.deepEqual(orderEventKey({ eventType: "Status:Confirmed", notes: null }), { key: "status", status: "Confirmed" });
    assert.deepEqual(orderEventKey({ eventType: "Payment:Cash", notes: "500" }), { key: "payment", method: "Cash", amount: "500" });
    assert.deepEqual(orderEventKey({ eventType: "Payment:Waived", notes: "gift" }), { key: "paymentStatus", status: "Waived" });
    assert.deepEqual(orderEventKey({ eventType: "ReservationExpired:Released", notes: null }), { key: "reservationReleased" });
    assert.deepEqual(orderEventKey({ eventType: "Mystery", notes: null }), { key: "other", type: "Mystery" });
  });

  it("puts a local Egyptian mobile in international form for WhatsApp", () => {
    assert.equal(customerWhatsAppNumber("01012345772"), "201012345772");
    assert.equal(customerWhatsAppNumber("4915112345678"), "4915112345678");
  });
});
