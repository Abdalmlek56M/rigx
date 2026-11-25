import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { specIconKey } from "./spec-icon-rules.ts";

describe("specIconKey", () => {
  it("gives the starter specs of the base categories a fitting icon", () => {
    const expected: Record<string, string> = {
      socket: "board", supported_sockets: "board", chipset: "board", form_factor: "board", m2_slots: "board",
      cores: "cpu", threads: "cpu", base_clock: "gauge", speed: "gauge", read_speed: "gauge", refresh_rate: "gauge",
      vram: "memory", cache: "memory", modules: "memory", memory_type: "layers", vram_type: "layers", storage_type: "layers",
      length: "ruler", max_length: "ruler", size: "ruler", case_size: "case", side_panel: "case", fan_size: "fan", fans_included: "fan",
      power_w: "power", efficiency: "power", tdp: "power", interface: "plug", power_connectors: "power", pcie_connectors: "plug",
      modular: "plug", capacity: "storage", panel: "monitor", resolution: "monitor", curved: "monitor", rgb: "sparkle",
      wifi: "wifi", integrated_gpu: "gpu", gpu_maker: "gpu", cooler_type: "cooling", radiator: "cooling", cooler_included: "cooling"
    };
    assert.deepEqual(Object.fromEntries(Object.keys(expected).map((key) => [key, specIconKey(key)])), expected);
  });

  it("falls back to the tag for an unknown key", () => {
    assert.equal(specIconKey("colour"), "tag");
  });
});
