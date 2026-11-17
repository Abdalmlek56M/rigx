import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { daysLeftToShow, lookupHref, MAX_QUERY_LENGTH, parseLookupQuery } from "./warranty-query.ts";

describe("parseLookupQuery", () => {
  it("trims, takes the first value and caps the length", () => {
    assert.equal(parseLookupQuery("  RX-0154 "), "RX-0154");
    assert.equal(parseLookupQuery(["a", "b"]), "a");
    assert.equal(parseLookupQuery(undefined), "");
    assert.equal(parseLookupQuery("x".repeat(200)).length, MAX_QUERY_LENGTH);
  });
});

describe("lookupHref", () => {
  it("encodes the text and drops an empty one", () => {
    assert.equal(lookupHref(" 0128 918 "), "/warranty?q=0128%20918");
    assert.equal(lookupHref("  "), "/warranty");
  });
});

describe("daysLeftToShow", () => {
  it("shows days only for an active warranty", () => {
    assert.equal(daysLeftToShow("Active", 28), 28);
    assert.equal(daysLeftToShow("Expired", 0), null);
    assert.equal(daysLeftToShow("Active", null), null);
  });
});
