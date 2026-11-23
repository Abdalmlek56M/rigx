import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { DONUT_COLORS, donutArcs, donutSlices } from "./donut-rules.ts";

const row = (key: string, value: number) => ({ key, label: key, value });

describe("donut slices", () => {
  it("orders the rows largest first, gives each a colour and its share of the total", () => {
    const slices = donutSlices([row("b", 100), row("a", 300)], "other");
    assert.deepEqual(slices.map((slice) => [slice.key, slice.color, slice.percent]), [["a", DONUT_COLORS[0], 75], ["b", DONUT_COLORS[1], 25]]);
  });

  it("leaves out rows with nothing in them", () => {
    assert.deepEqual(donutSlices([row("a", 0), row("b", 50)], "other").map((slice) => slice.key), ["b"]);
    assert.deepEqual(donutSlices([], "other"), []);
  });

  it("adds the smallest rows into one last quiet slice when there are more rows than colours", () => {
    const rows = Array.from({ length: 9 }, (_, index) => row(`r${index}`, 100 - index));
    const slices = donutSlices(rows, "Other");
    assert.equal(slices.length, DONUT_COLORS.length);
    const last = slices.at(-1);
    assert.deepEqual([last?.key, last?.label, last?.color], ["other", "Other", DONUT_COLORS.at(-1)]);
    assert.equal(last?.value, 94 + 93 + 92);
    assert.equal(Math.round(slices.reduce((sum, slice) => sum + slice.percent, 0)), 100);
  });

  it("uses each colour in turn for exactly as many rows as colours", () => {
    const slices = donutSlices(Array.from({ length: DONUT_COLORS.length }, (_, index) => row(`r${index}`, 10 + index)), "Other");
    assert.deepEqual(slices.map((slice) => slice.color), [...DONUT_COLORS]);
  });
});

describe("donut arcs", () => {
  it("lays the slices end to end around the circle with a gap between them", () => {
    const arcs = donutArcs([{ value: 1 }, { value: 3 }], 100, 2);
    assert.deepEqual(arcs, [{ length: 23, offset: 0 }, { length: 73, offset: -25 }]);
  });

  it("draws a single slice as the whole circle and nothing for an empty total", () => {
    assert.deepEqual(donutArcs([{ value: 5 }], 100), [{ length: 100, offset: 0 }]);
    assert.deepEqual(donutArcs([], 100), []);
    assert.deepEqual(donutArcs([{ value: 0 }], 100), []);
  });
});
