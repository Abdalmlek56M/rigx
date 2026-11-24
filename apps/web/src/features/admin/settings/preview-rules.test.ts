import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { placeWatermark, previewNumber, sampleInvoiceNumber } from "./preview-rules.ts";

describe("watermark placement (same maths as the server's WatermarkPlacement)", () => {
  it("sizes the logo by the photo's width and keeps its proportions", () => {
    assert.deepEqual(placeWatermark(1200, 900, 300, 100, "BottomRight", 20), { x: 1200 - 240 - 27, y: 900 - 80 - 27, width: 240, height: 80 });
  });

  it("puts it in the chosen corner, 3 % of the shorter side from the edges", () => {
    assert.deepEqual(placeWatermark(1000, 500, 100, 100, "TopLeft", 10), { x: 15, y: 15, width: 100, height: 100 });
    assert.deepEqual(placeWatermark(1000, 500, 100, 100, "TopRight", 10), { x: 885, y: 15, width: 100, height: 100 });
    assert.deepEqual(placeWatermark(1000, 500, 100, 100, "BottomLeft", 10), { x: 15, y: 385, width: 100, height: 100 });
    assert.deepEqual(placeWatermark(1000, 500, 100, 100, "Center", 10), { x: 450, y: 200, width: 100, height: 100 });
  });

  it("never lets a tall logo be taller than the photo", () => {
    const placed = placeWatermark(1000, 400, 100, 400, "Center", 50);
    assert.equal(placed.height, 400);
    assert.equal(placed.width, 100);
  });
});

describe("preview helpers", () => {
  it("reads the form's text as a number inside the allowed range", () => {
    assert.equal(previewNumber("40", 5, 100, 40), 40);
    assert.equal(previewNumber("", 5, 100, 40), 40);
    assert.equal(previewNumber("abc", 5, 100, 40), 40);
    assert.equal(previewNumber("500", 5, 100, 40), 100);
    assert.equal(previewNumber("1", 5, 100, 40), 5);
  });

  it("builds the sample invoice number like the server", () => {
    assert.equal(sampleInvoiceNumber("inv", 2026), "INV-2026-0001");
    assert.equal(sampleInvoiceNumber("  ", 2026), "INV-2026-0001");
    assert.equal(sampleInvoiceNumber("rx", 2027), "RX-2027-0001");
  });
});
