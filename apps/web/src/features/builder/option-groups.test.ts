import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { groupOptions } from "./option-groups.ts";

type Options = Parameters<typeof groupOptions>[0];

interface Spec {
  code: string;
  model?: string;
  condition?: string;
  alone?: number;
  inBuild?: number;
  available?: boolean;
  compatible?: boolean;
  thumb?: string | null;
}

const options = (specs: Spec[]): Options =>
  specs.map((spec) => ({
    item: {
      code: spec.code,
      modelName: spec.model ?? "RTX 4060",
      condition: spec.condition ?? "New",
      warrantyLabel: null,
      thumbnailStorageKey: spec.thumb ?? null,
      prices: { alone: spec.alone ?? 100, withBoard: 90, inBuild: spec.inBuild ?? 80 }
    },
    issues: [],
    isAvailable: spec.available ?? true,
    isCompatible: spec.compatible ?? true
  })) as unknown as Options;

describe("groupOptions", () => {
  it("puts new and used units of one model in one group, keyed case-insensitively", () => {
    const groups = groupOptions(options([{ code: "RX-1" }, { code: "RX-2", model: " rtx 4060 ", condition: "Used" }]), new Set(), false);
    assert.equal(groups.length, 1);
    assert.deepEqual([groups[0]?.new?.code, groups[0]?.used?.code], ["RX-1", "RX-2"]);
  });
  it("keeps first-appearance order and leaves a missing condition null", () => {
    const groups = groupOptions(options([{ code: "RX-1", model: "B" }, { code: "RX-2", model: "A" }]), new Set(), false);
    assert.deepEqual(groups.map((group) => group.modelName), ["B", "A"]);
    assert.equal(groups[0]?.used, null);
  });
  it("skips options with an unknown condition", () => {
    assert.deepEqual(groupOptions(options([{ code: "RX-1", condition: "Refurbished" }]), new Set(), false), []);
  });
  it("shows the in-build or the alone price", () => {
    const list = options([{ code: "RX-1", alone: 100, inBuild: 80 }]);
    assert.equal(groupOptions(list, new Set(), true)[0]?.new?.price, 80);
    assert.equal(groupOptions(list, new Set(), false)[0]?.new?.price, 100);
  });
  it("flags unavailable before incompatible, and never blocks the selected one", () => {
    const list = options([
      { code: "RX-1", available: false, compatible: false },
      { code: "RX-2", model: "B", compatible: false },
      { code: "RX-3", model: "C", available: false }
    ]);
    const groups = groupOptions(list, new Set(["RX-3"]), false);
    assert.equal(groups[0]?.new?.block, "unavailable");
    assert.equal(groups[1]?.new?.block, "incompatible");
    assert.deepEqual([groups[2]?.new?.selected, groups[2]?.new?.block], [true, null]);
  });
  it("prefers the selected unit, then pickable, then incompatible, then unavailable, then cheaper", () => {
    const pick = (specs: Spec[], selected: string[] = []) => groupOptions(options(specs), new Set(selected), false)[0]?.new?.code;
    assert.equal(pick([{ code: "RX-1", available: false }, { code: "RX-2", compatible: false }]), "RX-2");
    assert.equal(pick([{ code: "RX-1", compatible: false }, { code: "RX-2", alone: 500 }]), "RX-2");
    assert.equal(pick([{ code: "RX-1", alone: 300 }, { code: "RX-2", alone: 200 }]), "RX-2");
    assert.equal(pick([{ code: "RX-1", alone: 100 }, { code: "RX-2", alone: 900 }], ["RX-2"]), "RX-2");
  });
  it("takes the first thumbnail it finds and returns nothing for no options", () => {
    const groups = groupOptions(options([{ code: "RX-1" }, { code: "RX-2", thumb: "t2" }, { code: "RX-3", thumb: "t3" }]), new Set(), false);
    assert.equal(groups[0]?.thumbnailStorageKey, "t2");
    assert.deepEqual(groupOptions([], new Set(), false), []);
  });
});
