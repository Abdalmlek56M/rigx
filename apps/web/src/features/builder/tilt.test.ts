import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { clampTilt, tiltFromDrag, tiltFromPointer } from "./tilt.ts";

describe("tiltFromPointer", () => {
  it("is 0 in the centre and plus or minus 1 at the edges", () => {
    assert.equal(tiltFromPointer(200, 100, 200), 0);
    assert.equal(tiltFromPointer(100, 100, 200), -1);
    assert.equal(tiltFromPointer(300, 100, 200), 1);
  });

  it("stays between -1 and 1 outside the drawing", () => {
    assert.equal(tiltFromPointer(0, 100, 200), -1);
    assert.equal(tiltFromPointer(900, 100, 200), 1);
  });

  it("is 0 for no width", () => {
    assert.equal(tiltFromPointer(50, 0, 0), 0);
  });
});

describe("tiltFromDrag", () => {
  it("turns all the way over the full width", () => {
    assert.equal(tiltFromDrag(-1, 100, 200), 0);
    assert.equal(tiltFromDrag(-1, 200, 200), 1);
    assert.equal(tiltFromDrag(0, -100, 200), -1);
  });

  it("clamps, and keeps the start for no width", () => {
    assert.equal(tiltFromDrag(0.5, 400, 200), 1);
    assert.equal(tiltFromDrag(0.5, 40, 0), 0.5);
  });
});

describe("clampTilt", () => {
  it("clamps to -1..1 and rounds to three decimals", () => {
    assert.equal(clampTilt(2), 1);
    assert.equal(clampTilt(-5), -1);
    assert.equal(clampTilt(0.12349), 0.123);
    assert.equal(clampTilt(0), 0);
  });
});
