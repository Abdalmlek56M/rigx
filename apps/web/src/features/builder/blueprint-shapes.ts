/** One drawn piece of the case blueprint (a 240 × 300 drawing). `part` outlines are what a slot lights up; `detail` marks are drawn on top of them. */
export type Shape =
  | {
      kind: "rect";
      x: number;
      y: number;
      width: number;
      height: number;
      radius?: number;
      /** Thin pieces (memory sticks) keep a plain outline, since dashes would blur them. */
      solid?: boolean;
    }
  | { kind: "circle"; cx: number; cy: number; r: number }
  | { kind: "path"; d: string };

export interface SlotDrawing {
  /** The outline that lights up (and is clicked). */
  parts: readonly Shape[];
  /** Marks that belong to it: fans, the die of a processor, the chips of a memory stick. */
  details: readonly Shape[];
  /** Where the slot's name is written once a part is chosen (the middle of the text). */
  label: { x: number; y: number };
}

export const BLUEPRINT_WIDTH = 240;
export const BLUEPRINT_HEIGHT = 300;

/** The outline of the case itself: it is not a slot's part, so it is drawn first and never lights up on its own (the CASE slot lights its panel). */
export const CASE_OUTLINE: Shape = { kind: "rect", x: 16, y: 8, width: 208, height: 284, radius: 16 };

/** Drawing order is back to front: the board first, the cooler last. */
export const BLUEPRINT_ORDER = ["CASE", "BOARD", "PSU", "SSD", "RAM", "GPU", "CPU", "COOL"] as const;

const round = (value: number) => Math.round(value * 10) / 10;

/** The blades of a fan as curved strokes from the hub outwards; every fan of the drawing (card, case, cooler, power supply) is made by this one function. */
export function fanBlades(cx: number, cy: number, r: number, blades = 7): Shape {
  const strokes: string[] = [];
  for (let index = 0; index < blades; index += 1) {
    const angle = (2 * Math.PI * index) / blades;
    const point = (radius: number, turn: number) => `${round(cx + radius * Math.cos(angle + turn))} ${round(cy + radius * Math.sin(angle + turn))}`;
    strokes.push(`M${point(r * 0.3, 0)}Q${point(r * 0.75, 0.6)} ${point(r * 0.94, 1)}`);
  }
  return { kind: "path", d: strokes.join("") };
}

/** A whole fan: the frame, the blades and the hub. */
function fan(cx: number, cy: number, r: number, blades = 7): Shape[] {
  return [{ kind: "circle", cx, cy, r }, fanBlades(cx, cy, r, blades), { kind: "circle", cx, cy, r: round(r * 0.2) }];
}

/** Memory chips down a stick, so it reads as a module and not as a bar. */
function stickChips(x: number): Shape[] {
  return [52, 68, 84, 100].map((y) => ({ kind: "rect", x: x + 2, y, width: 4, height: 11, radius: 1 }));
}

const RAM_STICKS: Shape[] = [158, 168, 178, 188].map((x) => ({ kind: "rect", x, y: 44, width: 8, height: 84, radius: 2, solid: true }));

export const SLOT_DRAWINGS: Readonly<Record<string, SlotDrawing>> = {
  CASE: {
    parts: [{ kind: "rect", x: 24, y: 16, width: 192, height: 268, radius: 12 }],
    details: [
      // Three intake fans on the front, and the slots of the power-supply basement.
      ...[62, 110, 158].flatMap((cy) => fan(40, cy, 13, 7)),
      { kind: "path", d: "M36 244h60M36 252h60M36 260h60M36 268h60" }
    ],
    label: { x: 120, y: 30 }
  },
  BOARD: {
    parts: [{ kind: "rect", x: 60, y: 36, width: 144, height: 184, radius: 6 }],
    details: [
      // The long expansion slot under the card, and the chipset.
      { kind: "path", d: "M72 186h118" },
      { kind: "rect", x: 142, y: 194, width: 14, height: 14, radius: 2 }
    ],
    label: { x: 108, y: 138 }
  },
  PSU: {
    parts: [{ kind: "rect", x: 112, y: 230, width: 96, height: 48, radius: 6 }],
    details: [...fan(160, 252, 16, 9), { kind: "path", d: "M120 238h12M120 244h12M188 238h12M188 244h12" }],
    label: { x: 160, y: 275 }
  },
  SSD: {
    parts: [{ kind: "rect", x: 76, y: 194, width: 56, height: 18, radius: 3 }],
    details: [{ kind: "path", d: "M82 199h12M82 204h8M124 197v12" }],
    label: { x: 104, y: 207 }
  },
  RAM: { parts: RAM_STICKS, details: [158, 168, 178, 188].flatMap(stickChips), label: { x: 177, y: 140 } },
  GPU: {
    parts: [{ kind: "rect", x: 68, y: 146, width: 128, height: 32, radius: 4 }],
    // Three fans, and the power plug on the top edge.
    details: [...[92, 132, 172].flatMap((cx) => fan(cx, 162, 11, 7)), { kind: "rect", x: 150, y: 142, width: 20, height: 4, radius: 1 }],
    label: { x: 160, y: 190 }
  },
  CPU: {
    parts: [{ kind: "rect", x: 88, y: 54, width: 40, height: 40, radius: 5 }],
    details: [{ kind: "rect", x: 97, y: 63, width: 22, height: 22, radius: 2 }],
    label: { x: 108, y: 127 }
  },
  COOL: {
    // The cooler sits over the processor: its fan inside a ring of fins. The processor's name is written under it, since the cooler hides it.
    parts: [{ kind: "circle", cx: 108, cy: 74, r: 33 }],
    details: [{ kind: "circle", cx: 108, cy: 74, r: 28 }, ...fan(108, 74, 24, 9)],
    label: { x: 108, y: 116 }
  }
};
