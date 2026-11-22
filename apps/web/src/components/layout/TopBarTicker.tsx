"use client";

import { useEffect, useState } from "react";

const SWITCH_MS = 5000;

/**
 * The top strip's messages, one at a time: they sit in one grid cell (no jump in height); the one that leaves slides up and
 * fades, the next one rises into its place from below (`.ticker-in`). Rotation stops while the pointer is over the strip,
 * while the tab is hidden and for people who asked for less motion (they see the first message only).
 */
export function TopBarTicker({ messages }: { messages: readonly string[] }) {
  const [index, setIndex] = useState(0);
  const [rotated, setRotated] = useState(false);
  const [paused, setPaused] = useState(false);

  useEffect(() => {
    if (messages.length < 2 || paused || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const timer = window.setInterval(() => {
      if (document.hidden) return;
      setRotated(true);
      setIndex((current) => (current + 1) % messages.length);
    }, SWITCH_MS);
    return () => window.clearInterval(timer);
  }, [messages.length, paused]);

  return (
    <div
      className="grid min-w-0 items-center"
      onPointerEnter={() => setPaused(true)}
      onPointerLeave={() => setPaused(false)}
    >
      {messages.map((message, position) => {
        const current = position === index;
        return (
          <span
            key={message}
            aria-hidden={!current}
            className={`col-start-1 row-start-1 flex min-w-0 items-center gap-2 transition-[opacity,transform] duration-500 motion-reduce:transition-none ${current ? `opacity-100 ${rotated ? "ticker-in" : ""}` : "pointer-events-none -translate-y-2 opacity-0"}`}
          >
            <span aria-hidden="true" className="h-1.5 w-1.5 shrink-0 rounded-full bg-orange" />
            <span className="truncate">{message}</span>
          </span>
        );
      })}
    </div>
  );
}
