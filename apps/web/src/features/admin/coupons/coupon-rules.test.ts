import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  canDelete, couponForm, couponStatus, couponTotals, couponValid, EMPTY_COUPON_FORM, endsBeforeStart, generateCouponCode, normalizeCode, sortCoupons, toCouponInput
} from "./coupon-rules.ts";
import type { Coupon } from "./coupons-api.ts";

const coupon = (overrides: Partial<Coupon> = {}): Coupon => ({
  id: "1", code: "AHMED10", sourceName: "Ahmed", discountKind: "Percent", discountValue: 10, maxDiscount: 200, minOrderTotal: null, maxUses: null,
  oncePerCustomer: true, startsAtUtc: null, endsAtUtc: null, isActive: true, isRunning: true, uses: 0, completedUses: 0, revenue: 0,
  discountGiven: 0, lastUsedAtUtc: null, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: "2026-01-01T00:00:00Z", rowVersion: 3, ...overrides
});
const filled = { ...EMPTY_COUPON_FORM, code: "AHMED10", sourceName: "Ahmed", value: "10" };

describe("normalizeCode", () => {
  it("upper-cases and drops what a code cannot hold", () => {
    assert.equal(normalizeCode("ahmed 10!-x"), "AHMED10-X");
    assert.equal(normalizeCode("a".repeat(40)).length, 30);
  });
});

describe("generateCouponCode", () => {
  /** A random source that walks through fixed values, so a run is repeatable. */
  const sequence = (...values: number[]) => { let index = 0; return () => values[index++ % values.length] ?? 0; };

  it("makes a short code of letters and digits that cannot be mixed up", () => {
    const code = generateCouponCode([], sequence(0, 0.2, 0.4, 0.6, 0.8, 0.99));
    assert.match(code ?? "", /^[A-HJ-NP-Z2-9]{6}$/);
    assert.equal(normalizeCode(code ?? ""), code);
  });

  it("never returns a code that already exists, in any case", () => {
    const first = generateCouponCode([], sequence(0));
    assert.equal(first, "AAAAAA");
    const next = generateCouponCode(["aaaaaa"], sequence(0, 0, 0, 0, 0, 0, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5));
    assert.notEqual(next, "AAAAAA");
    assert.ok(next);
  });

  it("gives up with null when every try collides", () => {
    assert.equal(generateCouponCode(["AAAAAA"], sequence(0)), null);
  });
});

describe("couponValid", () => {
  it("accepts the minimum and rejects what the API would", () => {
    assert.equal(couponValid(filled), true);
    assert.equal(couponValid({ ...filled, code: "AB" }), false);
    assert.equal(couponValid({ ...filled, value: "91" }), false);
    assert.equal(couponValid({ ...filled, kind: "Amount", value: "150" }), true);
    assert.equal(couponValid({ ...filled, maxUses: "1.5" }), false);
    assert.equal(couponValid({ ...filled, startDate: "2026-02-02", endDate: "2026-02-01" }), false);
  });
  it("detects an end before the start", () => {
    assert.equal(endsBeforeStart({ startDate: "2026-02-02", endDate: "2026-02-01" }), true);
    assert.equal(endsBeforeStart({ startDate: "", endDate: "2026-02-01" }), false);
  });
});

describe("toCouponInput / couponForm", () => {
  it("nulls empty optionals and drops the cap for an amount", () => {
    const input = toCouponInput({ ...filled, kind: "Amount", maxDiscount: "50" }, 4);
    assert.equal(input.maxDiscount, null);
    assert.equal(input.maxUses, null);
    assert.equal(input.startsAtUtc, null);
    assert.equal(input.rowVersion, 4);
  });
  it("sends Cairo day bounds and round-trips", () => {
    const input = toCouponInput({ ...filled, startDate: "2026-11-20", endDate: "2026-11-21", maxUses: "5" }, 0);
    assert.equal(input.startsAtUtc, "2026-11-19T22:00:00.000Z");
    assert.equal(input.endsAtUtc, "2026-11-21T21:59:59.999Z");
    const back = couponForm(coupon({ startsAtUtc: input.startsAtUtc as string, endsAtUtc: input.endsAtUtc as string, maxUses: 5 }));
    assert.equal(back.startDate, "2026-11-20");
    assert.equal(back.endDate, "2026-11-21");
    assert.equal(back.maxUses, "5");
  });
});

describe("couponStatus", () => {
  const now = new Date("2026-06-01T00:00:00Z");
  it("tells why a code is not working", () => {
    assert.equal(couponStatus(coupon(), now), "running");
    assert.equal(couponStatus(coupon({ isRunning: false, isActive: false }), now), "off");
    assert.equal(couponStatus(coupon({ isRunning: false, startsAtUtc: "2026-07-01T00:00:00Z" }), now), "notStarted");
    assert.equal(couponStatus(coupon({ isRunning: false }), now), "ended");
  });
});

describe("report", () => {
  it("sorts by revenue then uses and totals what is visible", () => {
    const list = [coupon({ id: "a", revenue: 10, uses: 1, discountGiven: 1 }), coupon({ id: "b", revenue: 50, uses: 1, discountGiven: 5, completedUses: 1 })];
    assert.deepEqual(sortCoupons(list).map((c) => c.id), ["b", "a"]);
    assert.deepEqual(couponTotals(list), { uses: 2, completedUses: 1, revenue: 60, discountGiven: 6 });
    assert.equal(couponTotals([coupon({ revenue: null, discountGiven: null })]).revenue, 0);
  });
  it("deletes only an unused code", () => {
    assert.equal(canDelete(coupon()), true);
    assert.equal(canDelete(coupon({ uses: 2 })), false);
  });
});
