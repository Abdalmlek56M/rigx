import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { specRows } from "./spec-rows.ts";

const defs = [
  { key: "vram", name: "الذاكرة", options: [], unit: "GB", isFilter: true },
  { key: "bus", name: "الواجهة", options: [], unit: null, isFilter: false }
];

describe("specRows", () => {
  it("uses the category order and names, appending the unit to plain numbers", () => {
    const rows = specRows({ bus: "PCIe 4.0", vram: "12" }, defs);
    assert.deepEqual(rows.map((r) => [r.name, r.value]), [["الذاكرة", "12 GB"], ["الواجهة", "PCIe 4.0"]]);
  });
  it("keeps unknown keys as they are and skips empty values", () => {
    const rows = specRows({ vram: " ", length: "227 mm" }, defs);
    assert.deepEqual(rows.map((r) => [r.name, r.value]), [["length", "227 mm"]]);
  });
});
