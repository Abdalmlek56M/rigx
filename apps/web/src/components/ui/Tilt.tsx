"use client";

import type { PointerEvent, ReactNode } from "react";

/** The most a tile leans toward the pointer, in degrees. */
const MAX_DEGREES = 3;
const MOTION_OK = "(hover: hover) and (pointer: fine) and (prefers-reduced-motion: no-preference)";

/**
 * A wrapper that leans its content a few degrees toward the pointer (`.tilt` in globals.css reads the two CSS variables).
 * Mouse and pen only: touch screens and people who asked for less motion never see it.
 */
export function Tilt({ children, className = "" }: { children: ReactNode; className?: string }) {
  function lean(event: PointerEvent<HTMLDivElement>) {
    if (event.pointerType === "touch" || !window.matchMedia(MOTION_OK).matches) return;
    const box = event.currentTarget.getBoundingClientRect();
    const x = (event.clientX - box.left) / box.width - 0.5;
    const y = (event.clientY - box.top) / box.height - 0.5;
    event.currentTarget.style.setProperty("--tilt-x", `${(-y * MAX_DEGREES * 2).toFixed(2)}deg`);
    event.currentTarget.style.setProperty("--tilt-y", `${(x * MAX_DEGREES * 2).toFixed(2)}deg`);
  }

  function rest(event: PointerEvent<HTMLDivElement>) {
    event.currentTarget.style.removeProperty("--tilt-x");
    event.currentTarget.style.removeProperty("--tilt-y");
  }

  return (
    <div className={`tilt${className ? ` ${className}` : ""}`} onPointerMove={lean} onPointerLeave={rest}>
      {children}
    </div>
  );
}
