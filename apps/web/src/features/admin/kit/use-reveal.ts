"use client";

import { useEffect, useRef } from "react";

/** How long a newly picked panel may still grow (its data loading) and be scrolled to again. */
const SETTLE_MS = 1500;

/**
 * Brings a detail/edit panel into view when the picked row changes (`key`) and the panel starts off screen:
 * on a phone the panel stacks under the list, so a tap would otherwise seem to do nothing. No key → nothing.
 * A panel that is still loading is short, so the check runs again while it grows.
 */
export function useReveal<T extends HTMLElement>(key: string | null) {
  const ref = useRef<T>(null);
  useEffect(() => {
    const element = ref.current;
    if (!key || !element) return;
    const reveal = () => {
      const { top } = element.getBoundingClientRect();
      if (top >= 0 && top < window.innerHeight * 0.6) return;
      const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      element.scrollIntoView({ behavior: reduced ? "auto" : "smooth", block: "start" });
    };
    reveal();
    const observer = new ResizeObserver(reveal);
    observer.observe(element);
    const stop = window.setTimeout(() => observer.disconnect(), SETTLE_MS);
    return () => {
      observer.disconnect();
      window.clearTimeout(stop);
    };
  }, [key]);
  return ref;
}
