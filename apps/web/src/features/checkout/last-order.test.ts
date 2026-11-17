import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { parseLastOrder } from "./last-order.ts";

describe("parseLastOrder", () => {
  it("reads a stored order", () => {
    const order = parseLastOrder(JSON.stringify({ reference: "RIGX-1", trackingToken: "t", items: [] }));
    assert.equal(order?.reference, "RIGX-1");
  });
  it("ignores empty, malformed and foreign values", () => {
    assert.equal(parseLastOrder(null), null);
    assert.equal(parseLastOrder("{"), null);
    assert.equal(parseLastOrder("[]"), null);
    assert.equal(parseLastOrder(JSON.stringify({ reference: "x" })), null);
  });
});
