import assert from "node:assert/strict";
import { test } from "node:test";
import { warrantyText } from "./warranty-text.ts";

const arabic = (label: string) => `ضمان ${label}`;

test("a bare period gets the word warranty in front", () => {
  assert.equal(warrantyText("3 شهور", arabic), "ضمان 3 شهور");
  assert.equal(warrantyText("  سنة  ", arabic), "ضمان سنة");
});

test("a label that already names the warranty is kept as the owner wrote it", () => {
  assert.equal(warrantyText("ضمان الوكيل", arabic), "ضمان الوكيل");
  assert.equal(warrantyText("Agent warranty", (label) => `${label} warranty`), "Agent warranty");
});

test("no label is no warranty line", () => {
  assert.equal(warrantyText(null, arabic), null);
  assert.equal(warrantyText(undefined, arabic), null);
  assert.equal(warrantyText("   ", arabic), null);
});
