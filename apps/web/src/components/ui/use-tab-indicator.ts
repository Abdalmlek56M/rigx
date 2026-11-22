"use client";

import { useCallback, useEffect, useLayoutEffect, useRef } from "react";

/**
 * Slides one indicator under the live tab. Put the returned ref on the tab row (class `tabs-slide`), mark the live tab with
 * `data-active="true"` and render `<span className="tab-indicator" aria-hidden="true" />` first inside the row. The hook measures
 * the live tab into CSS variables on the row; the styles live in globals.css. `activeKey` changes whenever the live tab does.
 */
export function useTabIndicator<T extends HTMLElement>(activeKey: string | null | undefined) {
  const row = useRef<T>(null);

  const measure = useCallback(() => {
    const element = row.current;
    if (!element) return;
    const active = element.querySelector<HTMLElement>('[data-active="true"]');
    if (!active) {
      element.removeAttribute("data-indicator");
      return;
    }
    element.style.setProperty("--ind-x", `${active.offsetLeft}px`);
    element.style.setProperty("--ind-y", `${active.offsetTop}px`);
    element.style.setProperty("--ind-w", `${active.offsetWidth}px`);
    element.style.setProperty("--ind-h", `${active.offsetHeight}px`);
    if (element.dataset.indicator) return;
    // The first placement must not slide in from the corner: show it, then turn the transition on a frame later.
    element.dataset.indicator = "ready";
    requestAnimationFrame(() => requestAnimationFrame(() => {
      if (element.dataset.indicator) element.dataset.indicator = "slide";
    }));
  }, []);

  useLayoutEffect(measure, [measure, activeKey]);

  useEffect(() => {
    const element = row.current;
    if (!element) return;
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    void document.fonts?.ready.then(measure);
    return () => observer.disconnect();
  }, [measure]);

  return row;
}
