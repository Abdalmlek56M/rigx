/**
 * The vocabulary of the case drawing: a few kinds of shape, each with a role the stylesheet colours (`blueprint.css`), and the one function
 * that draws a fan. The picture is a layered SVG (a 320 × 380 drawing): the shell and its glass first, then the board, then the parts on it.
 */

/** What a mark is, so the stylesheet can paint it: a chassis edge, glass, a vent slot, a fan blade, a chip, a cable… */
export type ShapeRole = "shell" | "glass" | "glare" | "shroud" | "mount" | "vent" | "ring" | "blade" | "hub" | "chip" | "cable" | "screw" | "shadow" | "trace";

export type Shape = { role?: ShapeRole } & (
  | { kind: "rect"; x: number; y: number; width: number; height: number; radius?: number }
  | { kind: "circle"; cx: number; cy: number; r: number }
  | { kind: "path"; d: string }
);

/**
 * One place a part can sit: `parts` are the outlines that light up (and are clicked), `details` are the marks that belong to them (fans,
 * chips, cables). A slot that takes several pieces (drives, fans, sticks of memory) has one piece per place, listed in the order they fill.
 */
export interface Piece {
  parts: readonly Shape[];
  details: readonly Shape[];
  /** Where the name of this piece is written once it is filled (the middle of the text). */
  label?: { x: number; y: number };
}

export interface SlotDrawing {
  pieces: readonly Piece[];
  /** Where the slot's name is written once any piece is filled (the middle of the text); a slot with a label on every piece has none. */
  label?: { x: number; y: number };
}

export const BLUEPRINT_WIDTH = 320;
export const BLUEPRINT_HEIGHT = 380;

const round = (value: number) => Math.round(value * 10) / 10;

export const rect = (x: number, y: number, width: number, height: number, radius?: number, role?: ShapeRole): Shape => ({ kind: "rect", x, y, width, height, radius, role });
export const circle = (cx: number, cy: number, r: number, role?: ShapeRole): Shape => ({ kind: "circle", cx, cy, r, role });
export const path = (d: string, role?: ShapeRole): Shape => ({ kind: "path", d, role });

/** The blades of a fan as filled, curved lobes around the hub; every fan of the drawing (case, card, cooler, power supply) is made by this one function. */
export function fanBlades(cx: number, cy: number, r: number, blades = 7): Shape {
  const lobes: string[] = [];
  for (let index = 0; index < blades; index += 1) {
    const angle = (2 * Math.PI * index) / blades;
    const point = (radius: number, turn: number) => `${round(cx + radius * Math.cos(angle + turn))} ${round(cy + radius * Math.sin(angle + turn))}`;
    lobes.push(`M${point(r * 0.2, 0)}Q${point(r * 0.72, -0.42)} ${point(r * 0.94, 0.42)}Q${point(r * 0.62, 0.62)} ${point(r * 0.2, 0.5)}Z`);
  }
  return path(lobes.join(""), "blade");
}

/** A whole fan: the frame ring, the blades and the hub. */
export function fan(cx: number, cy: number, r: number, blades = 7): Shape[] {
  return [circle(cx, cy, r, "ring"), fanBlades(cx, cy, r, blades), circle(cx, cy, round(r * 0.2), "hub")];
}
