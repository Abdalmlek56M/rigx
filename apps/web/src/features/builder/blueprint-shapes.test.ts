import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { BLUEPRINT_HEIGHT, BLUEPRINT_ORDER, BLUEPRINT_WIDTH, SLOT_DRAWINGS, fanBlades, type Shape, type SlotDrawing } from "./blueprint-shapes.ts";

function bounds(shape: Shape): [number, number, number, number] {
  if (shape.kind === "rect") return [shape.x, shape.y, shape.x + shape.width, shape.y + shape.height];
  if (shape.kind === "circle") return [shape.cx - shape.r, shape.cy - shape.r, shape.cx + shape.r, shape.cy + shape.r];
  // Only absolute paths (the fan blades) are measured; the straight marks use relative moves.
  if (/[a-z]/.test(shape.d)) return [0, 0, 0, 0];
  const numbers = shape.d.match(/-?\d+(\.\d+)?/g)?.map(Number) ?? [];
  const xs = numbers.filter((_, index) => index % 2 === 0);
  const ys = numbers.filter((_, index) => index % 2 === 1);
  return [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
}

function drawing(slot: string): SlotDrawing {
  const found = SLOT_DRAWINGS[slot];
  assert.ok(found, slot);
  return found;
}

describe("case blueprint drawings", () => {
  it("draws every slot of the drawing order, with its label inside the picture", () => {
    for (const slot of BLUEPRINT_ORDER) {
      const { label } = drawing(slot);
      assert.ok(label.x > 0 && label.x < BLUEPRINT_WIDTH && label.y > 0 && label.y < BLUEPRINT_HEIGHT, `${slot} label`);
    }
  });

  it("keeps every part and detail inside the picture", () => {
    for (const [slot, drawing] of Object.entries(SLOT_DRAWINGS)) {
      for (const shape of [...drawing.parts, ...drawing.details]) {
        const [left, top, right, bottom] = bounds(shape);
        assert.ok(left >= 0 && top >= 0 && right <= BLUEPRINT_WIDTH && bottom <= BLUEPRINT_HEIGHT, `${slot} ${JSON.stringify(shape).slice(0, 40)}`);
      }
    }
  });

  it("puts the power supply at the bottom right and the cooler over the processor", () => {
    const [psuLeft, psuTop, psuRight] = bounds(drawing("PSU").parts[0] as Shape);
    assert.ok(psuLeft > BLUEPRINT_WIDTH / 2 - 20 && psuTop > BLUEPRINT_HEIGHT * 0.7 && psuRight > BLUEPRINT_WIDTH * 0.8);
    const [cpuLeft, cpuTop, cpuRight, cpuBottom] = bounds(drawing("CPU").parts[0] as Shape);
    const [coolLeft, coolTop, coolRight, coolBottom] = bounds(drawing("COOL").parts[0] as Shape);
    assert.ok(coolLeft <= cpuLeft && coolTop <= cpuTop && coolRight >= cpuRight && coolBottom >= cpuBottom);
  });

  it("gives a fan one curved blade per count, all finite numbers", () => {
    const blades = fanBlades(50, 50, 10, 7);
    assert.equal(blades.kind, "path");
    if (blades.kind === "path") {
      assert.equal(blades.d.split("M").length - 1, 7);
      assert.ok(!blades.d.includes("NaN"));
    }
  });

  it("draws the card's fans, the case's fans, the memory chips and the cooler's fan as blades", () => {
    const blades = (slot: string) => drawing(slot).details.filter((shape) => shape.kind === "path" && shape.d.includes("Q")).length;
    assert.equal(blades("GPU"), 3);
    assert.equal(blades("CASE"), 3);
    assert.equal(blades("COOL"), 1);
    assert.equal(blades("PSU"), 1);
    assert.equal(drawing("RAM").details.length, 16);
  });
});
