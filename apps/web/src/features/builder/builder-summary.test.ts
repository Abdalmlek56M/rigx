import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { EMPTY_EVALUATION, cartLinesOf, conditionCounts, partsSummaryText, powerShare, showsInBuildPrice, type Evaluation } from "./builder-summary.ts";

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
