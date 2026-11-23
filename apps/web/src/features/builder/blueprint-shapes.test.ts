import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { BLUEPRINT_HEIGHT, BLUEPRINT_WIDTH, fanBlades, type Shape, type SlotDrawing } from "./blueprint-geometry.ts";
import { BLUEPRINT_ORDER, FAN_MOUNTS, SLOT_DRAWINGS } from "./blueprint-shapes.ts";
import { MAX_QUANTITY, slotLimit } from "./slot-limits.ts";

type Box = [number, number, number, number];

function bounds(shape: Shape): Box {
  if (shape.kind === "rect") return [shape.x, shape.y, shape.x + shape.width, shape.y + shape.height];
  if (shape.kind === "circle") return [shape.cx - shape.r, shape.cy - shape.r, shape.cx + shape.r, shape.cy + shape.r];
  // Only absolute paths (the fan blades, the cables) are measured; the straight marks use relative moves.
  if (/[a-z]/.test(shape.d.replace(/[MLQZ]/g, ""))) return [0, 0, 0, 0];
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

const firstPart = (slot: string): Box => bounds(drawing(slot).pieces[0]!.parts[0]!);

const overlap = (a: Box, b: Box) => a[0] < b[2] && b[0] < a[2] && a[1] < b[3] && b[1] < a[3];

describe("case blueprint drawings", () => {
  it("draws every slot of the drawing order, and the order has every slot the builder knows", () => {
    assert.deepEqual([...BLUEPRINT_ORDER].sort(), ["BOARD", "CASE", "COOL", "CPU", "FAN", "GPU", "PSU", "RAM", "SSD"]);
    for (const slot of BLUEPRINT_ORDER) assert.ok(drawing(slot).pieces.length > 0, slot);
  });

  it("keeps every part, detail and label inside the picture", () => {
    for (const [slot, entry] of Object.entries(SLOT_DRAWINGS)) {
      const labels = [entry.label, ...entry.pieces.map((piece) => piece.label)].filter((label) => label !== undefined);
      for (const label of labels) assert.ok(label.x > 0 && label.x < BLUEPRINT_WIDTH && label.y > 0 && label.y < BLUEPRINT_HEIGHT, `${slot} label`);
      for (const shape of entry.pieces.flatMap((piece) => [...piece.parts, ...piece.details])) {
        const [left, top, right, bottom] = bounds(shape);
        assert.ok(left >= 0 && top >= 0 && right <= BLUEPRINT_WIDTH && bottom <= BLUEPRINT_HEIGHT, `${slot} ${JSON.stringify(shape).slice(0, 50)}`);
      }
    }
  });

  it("gives every slot a name on the drawing: one for the slot, or one on each piece (drives)", () => {
    for (const slot of BLUEPRINT_ORDER) {
      const entry = drawing(slot);
      assert.ok(entry.label !== undefined || entry.pieces.every((piece) => piece.label !== undefined), slot);
    }
  });

  it("has a place for as many pieces as the slot takes: three drives, four sticks of memory, six fans", () => {
    assert.equal(drawing("SSD").pieces.length, slotLimit("SSD").units);
    assert.equal(drawing("RAM").pieces.length, slotLimit("RAM").units);
    assert.equal(drawing("FAN").pieces.length, slotLimit("FAN").units);
    assert.equal(drawing("FAN").pieces.length, MAX_QUANTITY);
    for (const slot of ["CASE", "BOARD", "PSU", "GPU", "CPU", "COOL"]) assert.equal(drawing(slot).pieces.length, 1, slot);
  });

  it("puts the power supply at the bottom right and the cooler over the processor", () => {
    const [psuLeft, psuTop, psuRight] = firstPart("PSU");
    assert.ok(psuLeft > BLUEPRINT_WIDTH / 2 - 20 && psuTop > BLUEPRINT_HEIGHT * 0.75 && psuRight > BLUEPRINT_WIDTH * 0.8);
    const [cpuLeft, cpuTop, cpuRight, cpuBottom] = firstPart("CPU");
    const [coolLeft, coolTop, coolRight, coolBottom] = firstPart("COOL");
    assert.ok(coolLeft <= cpuLeft && coolTop <= cpuTop && coolRight >= cpuRight && coolBottom >= cpuBottom);
  });

  it("puts the parts on the board, and the board and the supply inside the case", () => {
    const board = firstPart("BOARD");
    const inside = (box: Box, around: Box) => box[0] >= around[0] && box[1] >= around[1] && box[2] <= around[2] && box[3] <= around[3];
    for (const slot of ["CPU", "COOL", "GPU"]) assert.ok(inside(firstPart(slot), board), slot);
    for (const piece of drawing("RAM").pieces) assert.ok(inside(bounds(piece.parts[0]!), board), "RAM");
    const shell = firstPart("CASE");
    for (const slot of ["BOARD", "PSU"]) assert.ok(inside(firstPart(slot), shell), slot);
  });

  it("keeps the drives, the fans and the board apart", () => {
    const board = firstPart("BOARD");
    const drives = drawing("SSD").pieces.map((piece) => bounds(piece.parts[0]!));
    const fans = drawing("FAN").pieces.map((piece) => bounds(piece.parts[0]!));
    const everyBox = [...drives, ...fans];
    for (const box of everyBox) assert.ok(!overlap(box, board), JSON.stringify(box));
    for (let first = 0; first < everyBox.length; first += 1)
      for (let second = first + 1; second < everyBox.length; second += 1) assert.ok(!overlap(everyBox[first]!, everyBox[second]!), `${first} and ${second}`);
  });

  it("has fan places on the front, the top and the back, filled in an order that keeps an intake and an exhaust", () => {
    assert.equal(FAN_MOUNTS.length, 6);
    const front = FAN_MOUNTS.filter((mount) => mount.cx < 90);
    const top = FAN_MOUNTS.filter((mount) => mount.cx >= 90 && mount.cx < 250);
    const back = FAN_MOUNTS.filter((mount) => mount.cx >= 250);
    assert.deepEqual([front.length, top.length, back.length], [3, 2, 1]);
    // One fan is on the front; two are the front and the back; the fourth is still the front.
    assert.deepEqual(FAN_MOUNTS.slice(0, 2).map((mount) => mount.cx < 90), [true, false]);
    assert.ok(FAN_MOUNTS.slice(0, 4).every((mount) => mount.cy > 55 && (mount.cx < 90 || mount.cx > 250)));
    // Every fan piece sits on its place, and the case has a frame there.
    drawing("FAN").pieces.forEach((piece, index) => {
      const [left, top2, right, bottom] = bounds(piece.parts[0]!);
      const mount = FAN_MOUNTS[index]!;
      assert.deepEqual([left, top2, right, bottom], [mount.cx - mount.r, mount.cy - mount.r, mount.cx + mount.r, mount.cy + mount.r]);
    });
    const frames = drawing("CASE").pieces[0]!.details.filter((shape) => shape.role === "mount");
    assert.equal(frames.length, FAN_MOUNTS.length);
  });

  it("fills the sticks of memory in the order a board's manual asks for", () => {
    const xs = drawing("RAM").pieces.map((piece) => bounds(piece.parts[0]!)[0]);
    assert.deepEqual(xs, [206, 226, 196, 216]);
  });

  it("gives a fan one curved blade per count, all finite numbers", () => {
    const blades = fanBlades(50, 50, 10, 7);
    assert.equal(blades.kind, "path");
    assert.equal(blades.role, "blade");
    if (blades.kind === "path") {
      assert.equal(blades.d.split("M").length - 1, 7);
      assert.ok(!blades.d.includes("NaN"));
    }
  });

  it("draws the card's, the cooler's and the supply's fans as blades, and a ring of them on every case fan", () => {
    const blades = (slot: string) => drawing(slot).pieces.flatMap((piece) => piece.details).filter((shape) => shape.role === "blade").length;
    assert.equal(blades("GPU"), 3);
    assert.equal(blades("COOL"), 1);
    assert.equal(blades("PSU"), 1);
    assert.equal(blades("FAN"), 6);
    assert.equal(drawing("CASE").pieces[0]!.details.filter((shape) => shape.role === "blade").length, 0);
    assert.equal(drawing("RAM").pieces.flatMap((piece) => piece.details).length, 16);
  });
});
