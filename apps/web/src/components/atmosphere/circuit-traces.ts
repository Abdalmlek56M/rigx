export interface Point {
  x: number;
  y: number;
}

/** One printed-circuit line: its corners, the distance along it at each corner and its total length (all in CSS pixels). */
export interface Trace {
  points: Point[];
  reach: number[];
  total: number;
}

/** The size of one grid square the lines snap to. */
export const CELL = 28;
/** East, north-east, north, … in turns of 45°; y grows downwards. */
const DIRECTIONS: readonly (readonly [number, number])[] = [[1, 0], [1, -1], [0, -1], [-1, -1], [-1, 0], [-1, 1], [0, 1], [1, 1]];
const MAX_TRACES = 70;
/** About one line per this many square pixels. */
const AREA_PER_TRACE = 16_000;

/** A small repeatable random generator (mulberry32), so the same size always draws the same board. */
export function seededRandom(seed: number): () => number {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let value = state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4_294_967_296;
  };
}

/**
 * The lines of a decorative board for an area: each runs straight, bends by 45° for a few squares, then turns a quarter and
 * runs on, like a real trace, and stops at the edge of the area.
 */
export function buildTraces(width: number, height: number, seed = 7): Trace[] {
  const random = seededRandom(seed);
  const columns = Math.floor(width / CELL);
  const rows = Math.floor(height / CELL);
  if (columns < 2 || rows < 2) return [];
  const count = Math.min(MAX_TRACES, Math.max(8, Math.round((width * height) / AREA_PER_TRACE)));
  const traces: Trace[] = [];

  for (let index = 0; index < count; index += 1) {
    let x = Math.floor(random() * (columns + 1));
    let y = Math.floor(random() * (rows + 1));
    let direction = Math.floor(random() * 4) * 2;
    const points: Point[] = [{ x: x * CELL, y: y * CELL }];

    /** Walks up to `cells` squares in the current direction without leaving the area; false when it could not move at all. */
    const walk = (cells: number): boolean => {
      const [dx, dy] = DIRECTIONS[direction] ?? [1, 0];
      let moved = 0;
      while (moved < cells && x + dx >= 0 && x + dx <= columns && y + dy >= 0 && y + dy <= rows) {
        x += dx;
        y += dy;
        moved += 1;
      }
      if (moved > 0) points.push({ x: x * CELL, y: y * CELL });
      return moved === cells;
    };

    const bends = 2 + Math.floor(random() * 3);
    for (let bend = 0; bend < bends; bend += 1) {
      if (!walk(2 + Math.floor(random() * 6))) break;
      const turn = random() < 0.5 ? 1 : -1;
      direction = (direction + turn + 8) % 8;
      if (!walk(1 + Math.floor(random() * 3))) break;
      direction = (direction + turn + 8) % 8;
    }

    if (points.length < 2) continue;
    const reach = [0];
    for (let at = 1; at < points.length; at += 1) {
      const from = points[at - 1] as Point;
      const to = points[at] as Point;
      reach.push((reach[at - 1] ?? 0) + Math.hypot(to.x - from.x, to.y - from.y));
    }
    traces.push({ points, reach, total: reach[reach.length - 1] ?? 0 });
  }
  return traces;
}

/** The point `distance` pixels along a trace (clamped to its ends). */
export function pointAt(trace: Trace, distance: number): Point {
  const wanted = Math.min(trace.total, Math.max(0, distance));
  let at = 1;
  while (at < trace.points.length - 1 && (trace.reach[at] ?? 0) < wanted) at += 1;
  const from = trace.points[at - 1] as Point;
  const to = trace.points[at] as Point;
  const start = trace.reach[at - 1] ?? 0;
  const span = (trace.reach[at] ?? start) - start;
  const share = span === 0 ? 0 : (wanted - start) / span;
  return { x: from.x + (to.x - from.x) * share, y: from.y + (to.y - from.y) * share };
}
