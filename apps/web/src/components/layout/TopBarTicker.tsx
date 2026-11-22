"use client";

import { useEffect, useState } from "react";

const SWITCH_MS = 5000;

/**
 * The top strip's messages, one at a time: they sit in one grid cell (no jump in height) and the current one fades up.
 * Rotation stops while the pointer is over the strip, while the tab is hidden and for people who asked for less motion
 * (they see the first message only).
 */
export function TopBarTicker({ messages }: { messages: readonly string[] }) {
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);

  useEffect(() => {
    if (messages.length < 2 || paused || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const timer = window.setInterval(() => {
      if (!document.hidden) setIndex((current) => (current + 1) % messages.length);
    }, SWITCH_MS);
    return () => window.clearInterval(timer);
  }, [messages.length, paused]);

  return (
    <div
      className="grid min-w-0 items-center"
      onPointerEnter={() => setPaused(true)}
      onPointerLeave={() => setPaused(false)}
    >
      {messages.map((message, position) => (
        <span
          key={message}
          aria-hidden={position !== index}
          className={`col-start-1 row-start-1 flex min-w-0 items-center gap-2 transition-[opacity,transform] duration-500 motion-reduce:transition-none ${position === index ? "translate-y-0 opacity-100" : "pointer-events-none translate-y-1.5 opacity-0"}`}
        >
          <span aria-hidden="true" className="h-1.5 w-1.5 shrink-0 rounded-full bg-orange" />
          <span className="truncate">{message}</span>
        </span>
      ))}
    </div>
  );
}
