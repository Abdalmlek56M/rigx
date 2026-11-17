import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { depositOf } from "./order-rules.ts";

describe("depositOf", () => {
  it("is what the upfront amount adds to the shipping fee", () => {
    assert.equal(depositOf(1700, 200), 1500);
  });
  it("is zero when only shipping is paid upfront or nothing at all", () => {
    assert.equal(depositOf(200, 200), 0);
    assert.equal(depositOf(0, 0), 0);
  });
});
