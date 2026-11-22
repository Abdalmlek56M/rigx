import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildTraces, CELL, pointAt, seededRandom } from "./circuit-traces.ts";

describe("buildTraces", () => {
  it("draws the same board for the same size and seed", () => {
    assert.deepEqual(buildTraces(900, 500, 3), buildTraces(900, 500, 3));
    assert.notDeepEqual(buildTraces(900, 500, 3), buildTraces(900, 500, 4));
  });

  it("keeps every corner inside the area and on the grid", () => {
    for (const trace of buildTraces(900, 500)) {
      for (const point of trace.points) {
        assert.ok(point.x >= 0 && point.x <= 900 && point.y >= 0 && point.y <= 500);
        assert.equal(point.x % CELL, 0);
        assert.equal(point.y % CELL, 0);
      }
    }
  });

  it("only runs straight or at 45 degrees", () => {
    for (const trace of buildTraces(1200, 600)) {
      for (let at = 1; at < trace.points.length; at += 1) {
        const dx = Math.abs((trace.points[at]?.x ?? 0) - (trace.points[at - 1]?.x ?? 0));
        const dy = Math.abs((trace.points[at]?.y ?? 0) - (trace.points[at - 1]?.y ?? 0));
        assert.ok(dx === 0 || dy === 0 || dx === dy, `${dx}x${dy}`);
      }
    }
  });

  it("scales the line count with the area and draws nothing on a tiny one", () => {
    assert.ok(buildTraces(1400, 700).length > buildTraces(400, 300).length);
    assert.deepEqual(buildTraces(20, 20), []);
  });

  it("has a growing distance along each trace", () => {
    for (const trace of buildTraces(900, 500)) {
      assert.ok(trace.reach.every((value, at) => at === 0 || value > (trace.reach[at - 1] ?? 0)));
      assert.equal(trace.total, trace.reach.at(-1));
    }
  });
});

describe("pointAt", () => {
  const trace = { points: [{ x: 0, y: 0 }, { x: 10, y: 0 }, { x: 10, y: 20 }], reach: [0, 10, 30], total: 30 };

  it("walks along the corners and clamps to the ends", () => {
    assert.deepEqual(pointAt(trace, 5), { x: 5, y: 0 });
    assert.deepEqual(pointAt(trace, 20), { x: 10, y: 10 });
    assert.deepEqual(pointAt(trace, -4), { x: 0, y: 0 });
    assert.deepEqual(pointAt(trace, 99), { x: 10, y: 20 });
  });
});

describe("seededRandom", () => {
  it("stays in [0, 1) and repeats", () => {
    const a = seededRandom(1);
    const b = seededRandom(1);
    for (let i = 0; i < 50; i += 1) {
      const value = a();
      assert.ok(value >= 0 && value < 1);
      assert.equal(value, b());
    }
  });
});
