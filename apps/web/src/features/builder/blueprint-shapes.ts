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
  /** Marks that belong to it: fans, the die of a processor, the contacts of a drive. */
  details: readonly Shape[];
}

export const BLUEPRINT_WIDTH = 240;
export const BLUEPRINT_HEIGHT = 300;

/** The outline of the case itself: it is not a slot's part, so it is drawn first and never lights up on its own (the CASE slot lights its panel). */
export const CASE_OUTLINE: Shape = { kind: "rect", x: 16, y: 8, width: 208, height: 284, radius: 16 };

/** Drawing order is back to front: the board first, the cooler last. */
export const BLUEPRINT_ORDER = ["CASE", "BOARD", "PSU", "SSD", "RAM", "GPU", "CPU", "COOL"] as const;

const RAM_STICKS: Shape[] = [160, 170, 180, 190].map((x) => ({ kind: "rect", x, y: 44, width: 7, height: 84, radius: 2, solid: true }));

export const SLOT_DRAWINGS: Readonly<Record<string, SlotDrawing>> = {
  CASE: {
    parts: [{ kind: "rect", x: 24, y: 16, width: 192, height: 268, radius: 12 }],
    details: [{ kind: "circle", cx: 120, cy: 28, r: 3 }]
  },
  BOARD: { parts: [{ kind: "rect", x: 40, y: 36, width: 160, height: 184, radius: 6 }], details: [] },
  PSU: {
    parts: [{ kind: "rect", x: 32, y: 232, width: 176, height: 44, radius: 6 }],
    details: [{ kind: "circle", cx: 172, cy: 254, r: 13 }, { kind: "path", d: "M172 241v26M159 254h26" }]
  },
  SSD: {
    parts: [{ kind: "rect", x: 56, y: 192, width: 64, height: 20, radius: 3 }],
    details: [{ kind: "path", d: "M64 200h24M64 205h16" }]
  },
  RAM: { parts: RAM_STICKS, details: [] },
  GPU: {
    parts: [{ kind: "rect", x: 52, y: 146, width: 136, height: 32, radius: 4 }],
    details: [
      { kind: "circle", cx: 90, cy: 162, r: 11 },
      { kind: "circle", cx: 126, cy: 162, r: 11 }
    ]
  },
  CPU: {
    parts: [{ kind: "rect", x: 98, y: 54, width: 44, height: 44, radius: 5 }],
    details: [{ kind: "rect", x: 108, y: 64, width: 24, height: 24, radius: 2 }]
  },
  COOL: {
    parts: [{ kind: "circle", cx: 120, cy: 76, r: 34 }],
    details: [{ kind: "path", d: "M120 50v52M94 76h52M101 57l38 38M139 57l-38 38" }]
  }
};
