"use client";

import { useSyncExternalStore } from "react";
import { THEME_STORAGE_KEY, themeCookie, type Theme } from "./theme-script";

export type { Theme };

function subscribe(onChange: () => void): () => void {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
  return () => observer.disconnect();
}

const read = (): Theme => (document.documentElement.dataset.theme === "dark" ? "dark" : "light");
const readServer = (): Theme => "light";

/** Applies a theme to the page and remembers it for the next visit (cookie for the server, local storage as the fallback). */
export function setTheme(next: Theme): void {
  document.documentElement.dataset.theme = next;
  // The cookie is what the server writes into the page, so a new page (a language switch) paints in the same theme.
  document.cookie = themeCookie(next);
  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, next);
  } catch {
    // Storage unavailable: the choice lasts for this page only.
  }
}

/** The page's current theme (the `data-theme` attribute), shared by every theme control. */
export function useTheme(): Theme {
  return useSyncExternalStore(subscribe, read, readServer);
}
