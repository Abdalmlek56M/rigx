import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { notificationHref, notificationIconKey, notificationText, unreadBadge } from "./notification-rules.ts";

const order = (status: string | null) => ({ kind: "OrderStatus", subject: "ORD-1001", status, target: "0193a7e2-0000-7000-8000-000000000001" });
const back = { kind: "BackInStock", subject: "RTX 4060", status: null, target: "RX-0042" };

describe("notificationText", () => {
  it("names the order status", () => {
    assert.deepEqual(notificationText(order("Shipped")), { key: "order.Shipped", values: { reference: "ORD-1001" } });
  });
  it("reads an unknown status as an update", () => {
    assert.equal(notificationText(order("Completed")).key, "order.Updated");
    assert.equal(notificationText(order(null)).key, "order.Updated");
  });
  it("names the item that is back", () => {
    assert.deepEqual(notificationText(back), { key: "backInStock", values: { item: "RTX 4060" } });
  });
  it("falls back for an unknown kind", () => {
    assert.equal(notificationText({ ...back, kind: "Promo" }).key, "other");
  });
});

describe("a store reply", () => {
  const reply = { kind: "RequestReply", subject: "REQ-0007", status: null, target: "REQ-0007" };
  it("names the request code and opens the customer's own requests", () => {
    assert.deepEqual(notificationText(reply), { key: "requestReply", values: { code: "REQ-0007" } });
    assert.equal(notificationHref(reply), "/account/requests");
  });
  it("picks a picture for every kind", () => {
    assert.equal(notificationIconKey(reply), "reply");
    assert.equal(notificationIconKey(order("Shipped")), "order");
    assert.equal(notificationIconKey(back), "stock");
    assert.equal(notificationIconKey({ ...back, kind: "Promo" }), "other");
  });
});

describe("notificationHref", () => {
  it("opens the order in the account", () => {
    assert.equal(notificationHref(order("Confirmed")), "/account/orders/0193a7e2-0000-7000-8000-000000000001");
  });
  it("opens the product by its code", () => {
    assert.equal(notificationHref(back), "/p/RX-0042");
  });
  it("has no link for an unknown kind", () => {
    assert.equal(notificationHref({ ...back, kind: "Promo" }), null);
  });
});

describe("unreadBadge", () => {
  it("hides at zero and caps at 9+", () => {
    assert.equal(unreadBadge(0), null);
    assert.equal(unreadBadge(3), "3");
    assert.equal(unreadBadge(12), "9+");
  });
});
