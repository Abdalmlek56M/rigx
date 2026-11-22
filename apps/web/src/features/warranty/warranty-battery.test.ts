import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { warrantyBattery } from "./warranty-battery.ts";

const START = "2026-01-01T00:00:00Z";
const END = "2026-04-11T00:00:00Z"; // 100 days

describe("warrantyBattery", () => {
  it("is full at the start, green above half", () => {
    assert.deepEqual(warrantyBattery("Active", 100, START, END), { charge: 1, tone: "good" });
    assert.equal(warrantyBattery("Active", 51, START, END)?.tone, "good");
  });

  it("turns amber from half down to a fifth and red below", () => {
    assert.equal(warrantyBattery("Active", 50, START, END)?.tone, "warn");
    assert.equal(warrantyBattery("Active", 21, START, END)?.tone, "warn");
    assert.equal(warrantyBattery("Active", 20, START, END)?.tone, "low");
    assert.equal(warrantyBattery("Active", 1, START, END)?.tone, "low");
  });

  it("is empty when expired, whatever the days say", () => {
    assert.deepEqual(warrantyBattery("Expired", 0, START, END), { charge: 0, tone: "empty" });
  });

  it("keeps the charge between 0 and 1", () => {
    assert.equal(warrantyBattery("Active", 500, START, END)?.charge, 1);
    assert.deepEqual(warrantyBattery("Active", 0, START, END), { charge: 0, tone: "empty" });
  });

  it("shows nothing for a void warranty, a missing end date or unknown days", () => {
    assert.equal(warrantyBattery("Void", 30, START, END), null);
    assert.equal(warrantyBattery("Active", 30, START, null), null);
    assert.equal(warrantyBattery("Active", null, START, END), null);
  });
});
