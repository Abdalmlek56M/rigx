import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canSetPaymentStatus,
  canTakePayment,
  canMarkUnreachable,
  canWaiveDeposit,
  canWaivePayment,
  completeBlock,
  customerWhatsAppNumber,
  orderWhatsAppNumber,
  eventPaymentAmount,
  filterOrders,
  matchesOrderTodo,
  orderTodoKey,
  nextStatus,
  nextStatusBlock,
  orderAddressText,
  orderEventKey,
  orderTimeline,
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

  it("hides the manual status select when nothing is left to pay", () => {
    assert.equal(canSetPaymentStatus(order()), true);
    assert.equal(canSetPaymentStatus(order({ remaining: 0 })), false);
    assert.equal(canSetPaymentStatus(order({ paymentStatus: "Paid" })), false);
    assert.equal(canSetPaymentStatus(order({ paymentStatus: "Waived" })), false);
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

describe("the dashboard's to-do lists", () => {
  it("lists orders on their way with money still due, as the dashboard counts them", () => {
    const todo = "awaitingPayment";
    assert.equal(matchesOrderTodo(order({ status: "Confirmed", paymentStatus: "Unpaid" }), todo), true);
    assert.equal(matchesOrderTodo(order({ status: "Delivered", paymentStatus: "PendingVerification" }), todo), true);
    assert.equal(matchesOrderTodo(order({ status: "New", paymentStatus: "Unpaid" }), todo), false);
    assert.equal(matchesOrderTodo(order({ status: "Confirmed", paymentStatus: "Paid" }), todo), false);
    assert.equal(matchesOrderTodo(order({ status: "Completed", paymentStatus: "Unpaid" }), todo), false);
  });

  it("lists ended reservations of orders that have not left the store", () => {
    const todo = "expiredReservations";
    assert.equal(matchesOrderTodo(order({ status: "Confirmed", reservationExpiredAtUtc: "2026-11-20T10:00:00Z" }), todo), true);
    assert.equal(matchesOrderTodo(order({ status: "Confirmed" }), todo), false);
    assert.equal(matchesOrderTodo(order({ status: "Cancelled", reservationExpiredAtUtc: "2026-11-20T10:00:00Z" }), todo), false);
  });

  it("keeps every order when no list is chosen and ignores an unknown name", () => {
    assert.equal(matchesOrderTodo(order(), ""), true);
    assert.equal(orderTodoKey("nonsense"), "");
    assert.equal(orderTodoKey("awaitingPayment"), "awaitingPayment");
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
    assert.deepEqual(orderEventKey({ eventType: "Contact:Unreachable", notes: null }), { key: "unreachable" });
    assert.deepEqual(orderEventKey({ eventType: "Mystery", notes: null }), { key: "other", type: "Mystery" });
  });

  it("puts a local Egyptian mobile in international form for WhatsApp", () => {
    assert.equal(customerWhatsAppNumber("01012345772"), "201012345772");
    assert.equal(customerWhatsAppNumber("4915112345678"), "4915112345678");
  });

  it("orders the history newest first and keeps equal times in order", () => {
    const events = [
      { id: "a", createdAtUtc: "2026-11-20T09:00:00Z" },
      { id: "b", createdAtUtc: "2026-11-21T09:00:00Z" },
      { id: "c", createdAtUtc: "2026-11-20T09:00:00Z" }
    ];
    assert.deepEqual(orderTimeline(events).map((event) => event.id), ["b", "a", "c"]);
  });

  it("messages the phone when it is on WhatsApp, else the separate number, else nobody", () => {
    assert.equal(orderWhatsAppNumber({ phoneNumber: "01012345772", phoneHasWhatsApp: true, whatsAppNumber: "01112223333" }), "201012345772");
    assert.equal(orderWhatsAppNumber({ phoneNumber: "01012345772", phoneHasWhatsApp: false, whatsAppNumber: "01112223333" }), "201112223333");
    assert.equal(orderWhatsAppNumber({ phoneNumber: "01012345772", phoneHasWhatsApp: false, whatsAppNumber: null }), null);
  });

  it("represents a payment amount only when the note is a number", () => {
    assert.equal(eventPaymentAmount("5500"), 5500);
    assert.equal(eventPaymentAmount("5500.5"), 5500.5);
    assert.equal(eventPaymentAmount("gift"), null);
    assert.equal(eventPaymentAmount(null), null);
  });
});

describe("address", () => {
  const name = (code: string) => (code === "alex" ? "الإسكندرية" : code);

  it("joins the parts present, in order, with the landmark after a separator", () => {
    assert.equal(orderAddressText({ governorate: "alex", area: "العجمي", street: "شارع 9", building: "12", landmark: "بجوار المسجد" }, name), "الإسكندرية › العجمي › شارع 9 › 12، بجوار المسجد");
    assert.equal(orderAddressText({ governorate: "alex", area: null, street: "شارع 9", building: null, landmark: null }, name), "الإسكندرية › شارع 9");
    assert.equal(orderAddressText(null, name), null);
  });
});

describe("could not reach the customer", () => {
  it("can be recorded on an open order only", () => {
    assert.equal(canMarkUnreachable({ status: "New" }), true);
    assert.equal(canMarkUnreachable({ status: "Shipped" }), true);
    assert.equal(canMarkUnreachable({ status: "Cancelled" }), false);
    assert.equal(canMarkUnreachable({ status: "Completed" }), false);
  });
});

describe("waiving only the deposit", () => {
  const base = { status: "Confirmed", paymentStatus: "Unpaid", depositAmount: 200, upfrontAmount: 250, paidAmount: 0 } as never;
  const with_ = (patch: object) => ({ ...(base as object), ...patch }) as never;

  it("is offered while the order is open and nothing of the deposit is paid", () => {
    assert.equal(canWaiveDeposit(base), true);
    assert.equal(canWaiveDeposit(with_({ paidAmount: 50 })), true);
  });

  it("is hidden once the deposit is touched, the order is closed or settled, or there is no deposit", () => {
    assert.equal(canWaiveDeposit(with_({ paidAmount: 100 })), false);
    assert.equal(canWaiveDeposit(with_({ status: "Cancelled" })), false);
    assert.equal(canWaiveDeposit(with_({ paymentStatus: "Paid" })), false);
    assert.equal(canWaiveDeposit(with_({ depositAmount: 0 })), false);
  });
});
