import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { badRequestOutcome } from "./checkout-errors.ts";

describe("badRequestOutcome", () => {
  it("maps a coupon failure to its problem, never to the raw text", () => {
    assert.deepEqual(badRequestOutcome({ couponCode: ["Expired"] }), { status: "coupon", problem: "Expired" });
  });
  it("lists other field messages", () => {
    assert.deepEqual(badRequestOutcome({ items: ["Sold out"], phone: ["Bad"] }), { status: "invalid", messages: ["Sold out", "Bad"] });
  });
  it("fails when there is nothing to show", () => {
    assert.deepEqual(badRequestOutcome({}), { status: "failed" });
  });
});
