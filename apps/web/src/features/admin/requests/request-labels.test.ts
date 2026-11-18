import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canClose,
  canDecide,
  canFollow,
  canMarkBackInStockSent,
  canOffer,
  convertAllowed,
  offersAllowed,
  requestStatusKey,
  requestTitle,
  requestTrail,
  requestTypeKey
} from "./request-labels.ts";

describe("request labels", () => {
  it("maps known types and statuses and falls back for unknown ones", () => {
    assert.equal(requestTypeKey("Sell"), "Sell");
    assert.equal(requestTypeKey("TradeIn"), "TradeIn");
    assert.equal(requestTypeKey("SomethingNew"), "Other");
    assert.equal(requestStatusKey("Offered"), "Offered");
    assert.equal(requestStatusKey("Weird"), "Other");
  });

  it("uses the model, then the code, then the subject, then the message as the row title", () => {
    assert.equal(requestTitle({ type: "Sell", modelName: "RTX 4060", itemCode: "RX-1", subject: null, message: null, code: "REQ-1" }), "RTX 4060");
    assert.equal(requestTitle({ type: "NotifyMe", modelName: null, itemCode: "RTX 4060", subject: null, message: null, code: "REQ-1" }), "RTX 4060");
    assert.equal(requestTitle({ type: "Contact", modelName: null, itemCode: null, subject: "warranty", message: "line one\nline two", code: "REQ-1" }), "warranty");
    assert.equal(requestTitle({ type: "Support", modelName: null, itemCode: null, subject: null, message: "first line\nsecond", code: "REQ-1" }), "first line");
    assert.equal(requestTitle({ type: "Support", modelName: null, itemCode: null, subject: null, message: null, code: "REQ-1" }), "REQ-1");
  });
});

describe("request actions", () => {
  it("only sell, trade-in and upgrade take offers", () => {
    assert.equal(offersAllowed("Sell"), true);
    assert.equal(offersAllowed("TradeIn"), true);
    assert.equal(offersAllowed("Upgrade"), true);
    assert.equal(offersAllowed("NotifyMe"), false);
    assert.equal(offersAllowed("Support"), false);
  });

  it("opens the offer form only in the statuses the API accepts", () => {
    assert.equal(canOffer("Sell", "New"), true);
    assert.equal(canOffer("Sell", "Following"), true);
    assert.equal(canOffer("Sell", "Offered"), true);
    assert.equal(canOffer("Sell", "Rejected"), true);
    assert.equal(canOffer("Sell", "Accepted"), false);
    assert.equal(canOffer("Sell", "Closed"), false);
    assert.equal(canOffer("Contact", "New"), false);
  });

  it("follows new or closed requests and closes any open one", () => {
    assert.equal(canFollow("New"), true);
    assert.equal(canFollow("Closed"), true);
    assert.equal(canFollow("Offered"), false);
    assert.equal(canClose("New"), true);
    assert.equal(canClose("Accepted"), true);
    assert.equal(canClose("Converted"), false);
    assert.equal(canClose("Closed"), false);
  });

  it("allows a decision only on an offer, and conversion only on an accepted sell/trade-in", () => {
    assert.equal(canDecide("Offered"), true);
    assert.equal(canDecide("Accepted"), false);
    assert.equal(convertAllowed("Sell", "Accepted"), true);
    assert.equal(convertAllowed("TradeIn", "Accepted"), true);
    assert.equal(convertAllowed("Upgrade", "Accepted"), false);
    assert.equal(convertAllowed("Sell", "Offered"), false);
  });

  it("marks a notify-me request sent only once it matched stock and was not messaged", () => {
    const base = { type: "NotifyMe", availableItemCode: "RX-1", notifiedAtUtc: null };
    assert.equal(canMarkBackInStockSent(base), true);
    assert.equal(canMarkBackInStockSent({ ...base, availableItemCode: null }), false);
    assert.equal(canMarkBackInStockSent({ ...base, notifiedAtUtc: "2026-11-18T10:00:00Z" }), false);
    assert.equal(canMarkBackInStockSent({ ...base, type: "Sell" }), false);
  });
});

describe("request trail", () => {
  it("shows the offer trail for offer types at the right step", () => {
    assert.deepEqual(requestTrail("Sell", "New")?.current, 0);
    assert.deepEqual(requestTrail("Sell", "Following")?.current, 0);
    assert.deepEqual(requestTrail("Sell", "Offered")?.current, 1);
    assert.deepEqual(requestTrail("Sell", "Accepted")?.current, 2);
    assert.deepEqual(requestTrail("Sell", "Converted")?.current, 3);
    assert.deepEqual(requestTrail("Sell", "Closed")?.current, 4);
    assert.deepEqual(requestTrail("Sell", "New")?.steps, ["New", "Offered", "Accepted", "Converted", "Closed"]);
  });

  it("has no trail for requests without an offer", () => {
    assert.equal(requestTrail("NotifyMe", "New"), null);
    assert.equal(requestTrail("Contact", "New"), null);
  });
});
