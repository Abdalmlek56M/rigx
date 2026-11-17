import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { addDays, cairoToday, changeDirection, niceMax, parsePeriod, periodRange, weeklyPoints, withShares } from "./dashboard-rules.ts";

describe("periods", () => {
  it("reads unknown values as this month", () => {
    assert.equal(parsePeriod("week"), "week");
    assert.equal(parsePeriod("decade"), "month");
  });
  it("uses the Cairo day", () => {
    assert.equal(cairoToday(new Date("2026-11-17T22:30:00Z")), "2026-11-18");
  });
  it("covers each period up to today", () => {
    const none = { from: "", to: "" };
    assert.deepEqual(periodRange("today", "2026-11-18", none), { from: "2026-11-18", to: "2026-11-18" });
    assert.deepEqual(periodRange("week", "2026-11-18", none), { from: "2026-11-12", to: "2026-11-18" });
    assert.deepEqual(periodRange("month", "2026-11-18", none), { from: "2026-11-01", to: "2026-11-18" });
    assert.deepEqual(periodRange("year", "2026-11-18", none), { from: "2026-01-01", to: "2026-11-18" });
  });
  it("accepts only a complete, ordered custom range", () => {
    assert.deepEqual(periodRange("custom", "2026-11-18", { from: "2026-10-01", to: "2026-10-31" }), { from: "2026-10-01", to: "2026-10-31" });
    assert.equal(periodRange("custom", "2026-11-18", { from: "2026-10-31", to: "2026-10-01" }), null);
    assert.equal(periodRange("custom", "2026-11-18", { from: "", to: "2026-10-01" }), null);
  });
  it("adds days across months", () => {
    assert.equal(addDays("2026-03-01", -1), "2026-02-28");
  });
});

describe("weeklyPoints", () => {
  it("sums weeks back from the last day", () => {
    const days = Array.from({ length: 9 }, (_, index) => ({ date: addDays("2026-11-01", index), grossSales: 10, netItemProfit: 1 }));
    assert.deepEqual(weeklyPoints(days), [
      { date: "2026-11-01", grossSales: 20, netItemProfit: 2 },
      { date: "2026-11-03", grossSales: 70, netItemProfit: 7 }
    ]);
    assert.deepEqual(weeklyPoints([]), []);
  });
});

describe("niceMax", () => {
  it("rounds up to 1, 2 or 5 × 10ⁿ", () => {
    assert.equal(niceMax(0), 0);
    assert.equal(niceMax(7), 10);
    assert.equal(niceMax(13_000), 20_000);
    assert.equal(niceMax(41_500), 50_000);
    assert.equal(niceMax(100), 100);
  });
});

describe("withShares", () => {
  it("adds the share of the total and the bar length against the largest", () => {
    const rows = withShares([{ n: 30 }, { n: 10 }], (row) => row.n);
    assert.deepEqual(rows, [{ n: 30, percent: 75, width: 100 }, { n: 10, percent: 25, width: (10 / 30) * 100 }]);
    assert.deepEqual(withShares([{ n: 0 }], (row) => row.n), [{ n: 0, percent: 0, width: 0 }]);
  });
});

describe("changeDirection", () => {
  it("is null without a previous value", () => {
    assert.equal(changeDirection(null), null);
    assert.equal(changeDirection(12.5), "up");
    assert.equal(changeDirection(-3), "down");
    assert.equal(changeDirection(0), "flat");
  });
});
