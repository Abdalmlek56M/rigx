import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { blueprintSlots, litPieces } from "./blueprint-state.ts";

const part = (slot: string, modelName: string, quantity = 1) => ({ slot, quantity, item: { modelName } }) as never;

describe("blueprintSlots", () => {
  it("draws only the slots the store has, back to front", () => {
    const drawn = blueprintSlots(["GPU", "CPU", "CASE", "BOARD", "ODD", "FAN", "SSD"], [], [], null);
    assert.deepEqual(drawn.map((entry) => entry.slot), ["CASE", "BOARD", "SSD", "GPU", "CPU", "FAN"]);
  });

  it("lights a slot that has a part and keeps the model for its tooltip", () => {
    const drawn = blueprintSlots(["CPU", "GPU"], [part("CPU", "Ryzen 5 5600")], [], null);
    assert.deepEqual(drawn.find((entry) => entry.slot === "CPU"), { slot: "CPU", filled: true, count: 1, active: false, issue: "none", models: ["Ryzen 5 5600"], model: "Ryzen 5 5600" });
    assert.deepEqual(drawn.find((entry) => entry.slot === "GPU"), { slot: "GPU", filled: false, count: 0, active: false, issue: "none", models: [], model: null });
  });

  it("counts the pieces of a slot by quantity and keeps every model (drives, fans)", () => {
    const drawn = blueprintSlots(["SSD", "FAN", "RAM"], [part("SSD", "A", 1), part("SSD", "B", 2), part("FAN", "F", 4), part("RAM", "R", 2)], [], null);
    const bySlot = Object.fromEntries(drawn.map((entry) => [entry.slot, [entry.count, entry.models]]));
    assert.deepEqual(bySlot, { SSD: [3, ["A", "B"]], RAM: [2, ["R"]], FAN: [4, ["F"]] });
  });

  it("marks the slot being chosen", () => {
    assert.deepEqual(blueprintSlots(["CPU", "GPU"], [], [], "GPU").map((entry) => [entry.slot, entry.active]), [["GPU", true], ["CPU", false]]);
  });

  it("tints both sides of a problem, and a conflict wins over a warning", () => {
    const issues = [
      { level: "Warning", slot: "PSU", otherSlot: null },
      { level: "Conflict", slot: "BOARD", otherSlot: "CPU" },
      { level: "Warning", slot: "CPU", otherSlot: "RAM" }
    ];
    const byslot = Object.fromEntries(blueprintSlots(["CPU", "BOARD", "RAM", "PSU", "GPU"], [], issues, null).map((entry) => [entry.slot, entry.issue]));
    assert.deepEqual(byslot, { BOARD: "conflict", PSU: "warning", RAM: "warning", CPU: "conflict", GPU: "none" });
  });
});

describe("litPieces", () => {
  it("lights one place per piece held, never more than the drawing has", () => {
    assert.equal(litPieces("FAN", 4), 4);
    assert.equal(litPieces("FAN", 9), 6);
    assert.equal(litPieces("SSD", 3), 3);
    assert.equal(litPieces("RAM", 2), 2);
    assert.equal(litPieces("CPU", 2), 1);
    assert.equal(litPieces("CPU", 0), 0);
    assert.equal(litPieces("UNKNOWN", 2), 0);
  });
});
