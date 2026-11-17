import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  invoiceKindKey, invoiceTokenFor, isActiveNav, itemsPreview, paymentMethodKey, requestStatusKey, requestTypeKey, warrantyStatusKey
} from "./account-labels.ts";

describe("label keys", () => {
  it("maps known request types and statuses, with safe fallbacks", () => {
    assert.equal(requestTypeKey("TradeIn"), "TradeIn");
    assert.equal(requestTypeKey("Whatever"), "Other");
    assert.equal(requestStatusKey("Offered"), "Offered");
    assert.equal(requestStatusKey("???"), "New");
  });
  it("maps warranty status, invoice kind and payment method", () => {
    assert.equal(warrantyStatusKey("Void"), "Void");
    assert.equal(warrantyStatusKey("Weird"), null);
    assert.equal(invoiceKindKey("Parts"), "Parts");
    assert.equal(invoiceKindKey("x"), null);
    assert.equal(paymentMethodKey("Wallet"), "Wallet");
    assert.equal(paymentMethodKey("Card"), null);
  });
});

describe("itemsPreview", () => {
  it("cuts the list and counts the rest", () => {
    assert.deepEqual(itemsPreview(["a", "b", "c", "d"], 2), { shown: ["a", "b"], more: 2 });
    assert.deepEqual(itemsPreview(["a"], 2), { shown: ["a"], more: 0 });
  });
});

describe("invoiceTokenFor", () => {
  const invoices = [
    { number: "INV-1", orderReference: "RIGX-1", accessToken: "t1" },
    { number: "INV-2", orderReference: "RIGX-1", accessToken: "t2" }
  ];
  it("prefers the invoice number, then the order", () => {
    assert.equal(invoiceTokenFor({ invoiceNumber: "INV-2", orderReference: "RIGX-1" }, invoices), "t2");
    assert.equal(invoiceTokenFor({ invoiceNumber: null, orderReference: "RIGX-1" }, invoices), "t1");
    assert.equal(invoiceTokenFor({ invoiceNumber: null, orderReference: "RIGX-9" }, invoices), null);
  });
});

describe("isActiveNav", () => {
  it("highlights the overview for order pages only", () => {
    assert.equal(isActiveNav("/account", "/account/orders/abc"), true);
    assert.equal(isActiveNav("/account", "/account/profile"), false);
    assert.equal(isActiveNav("/account/profile", "/account/profile"), true);
    assert.equal(isActiveNav("/account/builds", "/account/addresses"), false);
  });
});
