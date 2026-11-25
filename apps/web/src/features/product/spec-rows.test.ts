import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { SpecDef } from "../../lib/categories.ts";
import { cardSpecs, specRows } from "./spec-rows.ts";

const def = (key: string, type: string, extra: Partial<SpecDef> = {}): SpecDef => ({ key, name: key, options: [], unit: null, isFilter: false, type, ...extra });
const words = { yes: "نعم", no: "لا" };
const defs = [
  { ...def("vram", "Number", { unit: "GB", isFilter: true }), name: "الذاكرة" },
  { ...def("bus", "Text"), name: "الواجهة" }
];

describe("specRows", () => {
  it("uses the category order and names, appending the unit to plain numbers", () => {
    const rows = specRows({ bus: "PCIe 4.0", vram: "12" }, defs, words);
    assert.deepEqual(rows.map((r) => [r.name, r.value]), [["الذاكرة", "12 GB"], ["الواجهة", "PCIe 4.0"]]);
  });
  it("keeps unknown keys as they are and skips empty values", () => {
    const rows = specRows({ vram: " ", length: "227 mm" }, defs, words);
    assert.deepEqual(rows.map((r) => [r.name, r.value]), [["length", "227 mm"]]);
  });
  it("says yes and no in words", () => {
    const rows = specRows({ wifi: "yes", rgb: "no" }, [def("wifi", "YesNo"), def("rgb", "YesNo")], words);
    assert.deepEqual(rows.map((r) => r.value), ["نعم", "لا"]);
  });
});

describe("cardSpecs", () => {
  const cpu = [
    def("socket", "Choice", { isFilter: true }), def("cores", "Number", { isFilter: true }), def("integrated_gpu", "YesNo", { isFilter: true }),
    def("base_clock", "Number", { unit: "GHz" }), def("cache", "Number", { unit: "MB" }), def("ports", "Text")
  ];

  it("puts the filter specs first and skips yes/no and bare counts", () => {
    const rows = cardSpecs({ socket: "AM4", cores: "6", integrated_gpu: "no", base_clock: "3.6", cache: "32" }, cpu);
    assert.deepEqual(rows.map((r) => r.value), ["AM4", "3.6 GHz", "32 MB"]);
  });
  it("stops at the limit, leaves out long text and keys the category does not define", () => {
    const rows = cardSpecs({ ports: "HDMI 2.1, 3x DisplayPort 1.4a", base_clock: "4.2", colour: "black" }, cpu, 2);
    assert.deepEqual(rows.map((r) => r.value), ["4.2 GHz"]);
  });
  it("shows nothing for an item without specs", () => {
    assert.deepEqual(cardSpecs({}, cpu), []);
  });
});
