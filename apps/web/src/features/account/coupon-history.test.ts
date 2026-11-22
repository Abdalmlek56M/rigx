import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { totalSaved } from "./coupon-history.ts";

describe("totalSaved", () => {
  it("adds what every coupon took off", () => {
    assert.equal(totalSaved([{ discount: 100, orderStatus: "Delivered" }, { discount: 50.5, orderStatus: "New" }]), 150.5);
  });

  it("leaves out a cancelled order and counts nothing for none", () => {
    assert.equal(totalSaved([{ discount: 100, orderStatus: "Cancelled" }, { discount: 40, orderStatus: "Completed" }]), 40);
    assert.equal(totalSaved([]), 0);
  });
});
