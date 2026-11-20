import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { couponProblemFromFields, couponProblemKey } from "./coupon-problem.ts";

describe("couponProblemKey", () => {
  it("keeps a known code and turns an unknown one into Other", () => {
    assert.equal(couponProblemKey("Expired"), "Expired");
    assert.equal(couponProblemKey("Whatever"), "Other");
  });
});

describe("couponProblemFromFields", () => {
  it("reads the coupon field of a 400", () => {
    assert.equal(couponProblemFromFields({ couponCode: ["AlreadyUsed"] }), "AlreadyUsed");
    assert.equal(couponProblemFromFields({ CouponCode: ["Nope"] }), "Other");
  });
  it("is null when the coupon is not the problem", () => {
    assert.equal(couponProblemFromFields({ phone: ["bad"] }), null);
    assert.equal(couponProblemFromFields({}), null);
  });
});
