import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { blacklistEntry, customerTab, governorateName, monthYear, orderChannelKey, warrantyStateKey } from "./customer-rules.ts";

describe("customer helpers", () => {
  it("falls back to the first tab for an unknown one", () => {
    assert.equal(customerTab("warranties"), "warranties");
    assert.equal(customerTab("weird"), "orders");
  });

  it("names the month and year in the visitor's language", () => {
    assert.equal(monthYear("2026-03-14T10:00:00Z", "en"), "March 2026");
    assert.match(monthYear("2026-03-14T10:00:00Z", "ar"), /2026/);
  });

  it("maps a governorate code to its name and leaves a saved name alone", () => {
    const rates = [{ code: "C", name: "القاهرة" }, { code: "ALX", name: "الإسكندرية" }];
    assert.equal(governorateName(rates, "ALX"), "الإسكندرية");
    assert.equal(governorateName(rates, "الإسكندرية"), "الإسكندرية");
    assert.equal(governorateName(rates, null), null);
  });

  it("finds the blacklist entry by phone digits or email", () => {
    const entries = [
      { phoneNumber: null, email: "a@example.com" },
      { phoneNumber: "0100 000 0001", email: null }
    ];
    assert.equal(blacklistEntry(entries, "01000000001", null), entries[1]);
    assert.equal(blacklistEntry(entries, null, "A@EXAMPLE.COM"), entries[0]);
    assert.equal(blacklistEntry(entries, "01000000009", "x@example.com"), null);
  });

  it("maps the order channel and warranty state with a fallback", () => {
    assert.equal(orderChannelKey("Online"), "Online");
    assert.equal(orderChannelKey("Pickup"), "Other");
    assert.equal(warrantyStateKey("Active"), "Active");
    assert.equal(warrantyStateKey("Something"), "Other");
  });
});
