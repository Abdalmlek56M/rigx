import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { blueprintSlots } from "./blueprint-state.ts";

const part = (slot: string, modelName: string) => ({ slot, item: { modelName } }) as never;

describe("blueprintSlots", () => {
  it("draws only the slots the store has, back to front", () => {
    const drawn = blueprintSlots(["GPU", "CPU", "CASE", "BOARD", "ODD"], [], [], null);
    assert.deepEqual(drawn.map((entry) => entry.slot), ["CASE", "BOARD", "GPU", "CPU"]);
  });

  it("lights a slot that has a part and keeps the model for its tooltip", () => {
    const drawn = blueprintSlots(["CPU", "GPU"], [part("CPU", "Ryzen 5 5600")], [], null);
    assert.deepEqual(drawn.find((entry) => entry.slot === "CPU"), { slot: "CPU", filled: true, active: false, issue: "none", model: "Ryzen 5 5600" });
    assert.deepEqual(drawn.find((entry) => entry.slot === "GPU"), { slot: "GPU", filled: false, active: false, issue: "none", model: null });
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
