"use client";

import { useCallback, useEffect, useRef, type PointerEvent } from "react";
import { tiltFromDrag, tiltFromPointer } from "./tilt";

const REDUCED = "(prefers-reduced-motion: reduce)";

/**
 * A light turn of the case drawing: it follows a mouse over it, or a sideways finger drag on a phone (vertical scrolling is left to the page),
 * and eases back to the front when the pointer leaves or the finger lifts. It writes one CSS variable (`--tilt`) in an animation frame, so the
 * page does not re-render while it moves; nothing happens under reduced motion.
 */
export function useTilt<T extends HTMLElement>() {
  const ref = useRef<T>(null);
  const frame = useRef(0);
  const drag = useRef<{ x: number; tilt: number } | null>(null);
  const current = useRef(0);

  const apply = useCallback((value: number, moving: boolean) => {
    current.current = value;
    cancelAnimationFrame(frame.current);
    frame.current = requestAnimationFrame(() => {
      const element = ref.current;
      if (!element) return;
      element.style.setProperty("--tilt", String(value));
      element.dataset.moving = moving ? "true" : "false";
    });
  }, []);

  useEffect(() => () => cancelAnimationFrame(frame.current), []);

  const still = () => window.matchMedia(REDUCED).matches;

  return {
    ref,
    onPointerDown(event: PointerEvent<T>) {
      if (event.pointerType !== "mouse" && !still()) drag.current = { x: event.clientX, tilt: current.current };
    },
    onPointerMove(event: PointerEvent<T>) {
      if (still() || !ref.current) return;
      const box = ref.current.getBoundingClientRect();
      if (event.pointerType === "mouse") apply(tiltFromPointer(event.clientX, box.left, box.width), true);
      else if (drag.current) apply(tiltFromDrag(drag.current.tilt, event.clientX - drag.current.x, box.width), true);
    },
    onPointerLeave() {
      drag.current = null;
      apply(0, false);
    },
    onPointerUp(event: PointerEvent<T>) {
      if (event.pointerType === "mouse") return;
      drag.current = null;
      apply(0, false);
    },
    onPointerCancel() {
      drag.current = null;
      apply(0, false);
    }
  };
}
