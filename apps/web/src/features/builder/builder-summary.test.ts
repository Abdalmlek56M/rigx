import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { EMPTY_EVALUATION, cartLinesOf, conditionCounts, partsSummaryText, powerAdvice, powerFillPercent, powerShare, showsInBuildPrice, type Evaluation } from "./builder-summary.ts";

type Parts = Evaluation["parts"];

const part = (slot: string, modelName: string, condition: string, quantity = 1, code = "RX-1", unitPrice = 100) =>
  ({
    slot, quantity, unitPrice, lineTotal: unitPrice * quantity, isAvailable: true,
    item: { code, modelName, condition, warrantyLabel: "سنة", thumbnailStorageKey: "t", available: 3, specs: {} }
  }) as unknown as Parts[number];

describe("showsInBuildPrice", () => {
  it("needs both a CPU and a board, counting the slot being picked", () => {
    assert.equal(showsInBuildPrice(["CPU"], "BOARD"), true);
    assert.equal(showsInBuildPrice(["BOARD"], "CPU"), true);
    assert.equal(showsInBuildPrice(["CPU", "BOARD"], "GPU"), true);
    assert.equal(showsInBuildPrice(["CPU"], "GPU"), false);
    assert.equal(showsInBuildPrice([], "CPU"), false);
  });
});

describe("conditionCounts", () => {
  it("adds quantities per condition and skips unknown ones", () => {
    assert.deepEqual(conditionCounts([part("CPU", "a", "New", 2), part("GPU", "b", "used"), part("RAM", "c", "Weird", 5)]), { New: 2, Used: 1 });
    assert.deepEqual(conditionCounts([]), { New: 0, Used: 0 });
  });
});

describe("powerShare", () => {
  it("is the estimate over capacity, so 80% and 100% are the exact thresholds", () => {
    assert.equal(powerShare({ powerEstimateWatts: 400, psuCapacityWatts: 500 }), 0.8);
    assert.equal(powerShare({ powerEstimateWatts: 500, psuCapacityWatts: 500 }), 1);
    assert.ok((powerShare({ powerEstimateWatts: 600, psuCapacityWatts: 500 }) ?? 0) > 1);
    assert.equal(powerShare({ powerEstimateWatts: 0, psuCapacityWatts: 500 }), 0);
  });
  it("is null without both numbers or with no capacity", () => {
    assert.equal(powerShare({ powerEstimateWatts: null, psuCapacityWatts: 500 }), null);
    assert.equal(powerShare({ powerEstimateWatts: 400, psuCapacityWatts: null }), null);
    assert.equal(powerShare({ powerEstimateWatts: 400, psuCapacityWatts: 0 }), null);
  });
});

describe("cartLinesOf", () => {
  it("makes an item line per part at the evaluation's price option", () => {
    const evaluation: Evaluation = { ...EMPTY_EVALUATION, priceOption: "InBuild", parts: [part("CPU", "Ryzen 5", "New", 2, "RX-1", 250), part("GPU", "RTX", "Used", 1, "RX-2", 900)] };
    const lines = cartLinesOf(evaluation);
    assert.deepEqual(lines.map((line) => [line.kind, line.code, line.priceOption, line.unitPrice, line.quantity, line.condition, line.maxQuantity]), [
      ["Item", "RX-1", "InBuild", 250, 2, "New", 3],
      ["Item", "RX-2", "InBuild", 900, 1, "Used", 3]
    ]);
  });
  it("skips parts with an unknown condition and handles no parts", () => {
    assert.equal(cartLinesOf({ ...EMPTY_EVALUATION, parts: [part("CPU", "x", "Weird")] }).length, 0);
    assert.deepEqual(cartLinesOf(EMPTY_EVALUATION), []);
  });
});

describe("partsSummaryText", () => {
  const label = (condition: string) => (condition === "New" ? "جديد" : "مستعمل");
  it("writes one line per part with condition and quantity", () => {
    const text = partsSummaryText([part("CPU", "Ryzen 5 5600", "New"), part("RAM", "DDR4", "Used", 2)], { CPU: "المعالج" }, label);
    assert.equal(text, "المعالج: Ryzen 5 5600 (جديد)\nRAM: DDR4 (مستعمل) ×2");
  });
  it("omits the label for an unknown condition and is empty for no parts", () => {
    assert.equal(partsSummaryText([part("CPU", "X", "Weird")], {}, label), "CPU: X");
    assert.equal(partsSummaryText([], {}, label), "");
  });
});

describe("powerAdvice", () => {
  const advice = (estimate: number | null, capacity: number | null) => powerAdvice({ powerEstimateWatts: estimate, psuCapacityWatts: capacity });

  it("says nothing without an estimate", () => {
    assert.deepEqual(advice(null, null), { state: "unknown", estimate: null, capacity: null, share: null, recommended: null });
    assert.equal(advice(null, 650).state, "unknown");
  });

  it("asks for a supply when none is chosen and names an output that leaves the headroom (a multiple of 50 W)", () => {
    assert.deepEqual(advice(412, null), { state: "noSupply", estimate: 412, capacity: null, share: null, recommended: 550 });
    assert.equal(advice(400, null).recommended, 500);
    assert.equal(advice(401, null).recommended, 550);
  });

  it("is ok up to the warning share, near above it and over past the output of the supply", () => {
    assert.deepEqual(advice(400, 650), { state: "ok", estimate: 400, capacity: 650, share: 400 / 650, recommended: null });
    assert.equal(advice(520, 650).state, "ok");
    assert.equal(advice(521, 650).state, "near");
    assert.equal(advice(650, 650).state, "near");
    assert.equal(advice(651, 650).state, "over");
  });

  it("recommends a larger supply when it is near or over", () => {
    assert.equal(advice(600, 650).recommended, 750);
    assert.equal(advice(700, 650).recommended, 900);
  });
});

describe("powerFillPercent", () => {
  it("is the share of the supply in percent, capped at the end of the bar, and empty without a supply", () => {
    assert.equal(powerFillPercent(powerAdvice({ powerEstimateWatts: 325, psuCapacityWatts: 650 })), 50);
    assert.equal(powerFillPercent(powerAdvice({ powerEstimateWatts: 900, psuCapacityWatts: 650 })), 100);
    assert.equal(powerFillPercent(powerAdvice({ powerEstimateWatts: 300, psuCapacityWatts: null })), 0);
  });
});
