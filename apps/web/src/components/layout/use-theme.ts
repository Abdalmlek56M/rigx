"use client";

import { useSyncExternalStore } from "react";
import { THEME_STORAGE_KEY } from "./theme-script";

export type Theme = "light" | "dark";

function subscribe(onChange: () => void): () => void {
  const observer = new MutationObserver(onChange);
  observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
  return () => observer.disconnect();
}

const read = (): Theme => (document.documentElement.dataset.theme === "dark" ? "dark" : "light");
const readServer = (): Theme => "light";

/** Applies a theme to the page and remembers it for the next visit. */
export function setTheme(next: Theme): void {
  document.documentElement.dataset.theme = next;
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
