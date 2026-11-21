"use client";

import { useEffect } from "react";
import { clearCatalogPending, markCatalogPending } from "./catalog-pending";

const SLOW_MS = 5000;

/** The list query as the server rendered it; a change means the new results are on screen. */
interface CatalogClickTrackerProps {
  queryKey: string;
}

/**
 * One document-level listener for the catalog and builds lists: a link that only changes the query (a filter, a tab or a
 * page) marks the current list as loading, so the old results dim instead of jumping while the next page arrives. A new
 * query (a new `queryKey`) clears the dimming.
 */
export function CatalogClickTracker({ queryKey }: CatalogClickTrackerProps) {
  useEffect(() => {
    clearCatalogPending();
    let timer: ReturnType<typeof setTimeout> | undefined;

    function onClick(event: MouseEvent) {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
      const anchor = (event.target as Element | null)?.closest("a");
      if (!anchor || anchor.target === "_blank" || anchor.hasAttribute("download")) return;
      const url = new URL(anchor.href, window.location.href);
      if (url.origin !== window.location.origin || url.pathname !== window.location.pathname || url.search === window.location.search) return;
      markCatalogPending(`${url.pathname}${url.search}`);
      clearTimeout(timer);
      timer = setTimeout(clearCatalogPending, SLOW_MS);
    }

    document.addEventListener("click", onClick, true);
    return () => {
      document.removeEventListener("click", onClick, true);
      clearTimeout(timer);
    };
  }, [queryKey]);

  return null;
}
