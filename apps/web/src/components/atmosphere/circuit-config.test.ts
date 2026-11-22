import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { circuitMode, hexToRgb } from "./circuit-config.ts";

describe("circuitMode", () => {
  const capable = { reducedMotion: false, saveData: false, cores: 8, narrow: false };

  it("animates on a capable device", () => {
    assert.equal(circuitMode(capable), "live");
    assert.equal(circuitMode({ ...capable, cores: 0 }), "live");
  });

  it("is a still picture for reduced motion, phone-sized screens and low-end devices", () => {
    assert.equal(circuitMode({ ...capable, reducedMotion: true }), "static");
    assert.equal(circuitMode({ ...capable, narrow: true }), "static");
    assert.equal(circuitMode({ ...capable, cores: 4 }), "static");
    assert.equal(circuitMode({ ...capable, cores: 2 }), "static");
  });

  it("is off when the visitor saves data, whatever else is true", () => {
    assert.equal(circuitMode({ ...capable, saveData: true }), "off");
    assert.equal(circuitMode({ reducedMotion: true, saveData: true, cores: 2, narrow: true }), "off");
  });
});

describe("hexToRgb", () => {
  it("reads a six-digit hex token", () => {
    assert.deepEqual(hexToRgb("#ff6a2b"), [255, 106, 43]);
    assert.deepEqual(hexToRgb(" #0e0f12 "), [14, 15, 18]);
  });

  it("falls back to grey for anything else", () => {
    assert.deepEqual(hexToRgb("orange"), [200, 200, 200]);
  });
});
