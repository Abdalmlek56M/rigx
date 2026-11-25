/**
 * How far the case drawing turns: -1 (its start side away) to 1, from where the pointer is over it (a mouse) or how far a finger dragged
 * (a phone, from where the drag began). The stylesheet turns -1..1 into about ±11°, so the picture never turns more than a glance.
 */
export function tiltFromPointer(pointerX: number, left: number, width: number): number {
  if (width <= 0) return 0;
  return clampTilt(((pointerX - left) / width) * 2 - 1);
}

/** A finger drag across the drawing: a drag over its full width turns it all the way from where it started. */
export function tiltFromDrag(startTilt: number, dragX: number, width: number): number {
  if (width <= 0) return startTilt;
  return clampTilt(startTilt + (dragX / width) * 2);
}

export const clampTilt = (value: number): number => Math.round(Math.min(1, Math.max(-1, value)) * 1000) / 1000;
