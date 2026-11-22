import { parseStoredLines, type CartLine } from "./cart-rules.ts";

const STORAGE_KEY = "rigx.cart.v1";
const EMPTY: CartLine[] = [];

let lines: CartLine[] | undefined;
const listeners = new Set<() => void>();

function load(): CartLine[] {
  try {
    return parseStoredLines(window.localStorage.getItem(STORAGE_KEY));
  } catch {
    return EMPTY;
  }
}

function notify() {
  listeners.forEach((listener) => listener());
}

function onStorage(event: StorageEvent) {
  if (event.key !== STORAGE_KEY && event.key !== null) return;
  lines = load();
  notify();
}

/** The cart lives in memory and mirrors localStorage; without storage it still works for the page's lifetime. */
export const cartStore = {
  getSnapshot(): CartLine[] {
    lines ??= load();
    return lines;
  },
  getServerSnapshot(): CartLine[] {
    return EMPTY;
  },
  subscribe(listener: () => void): () => void {
    if (listeners.size === 0) window.addEventListener("storage", onStorage);
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
      if (listeners.size === 0) window.removeEventListener("storage", onStorage);
    };
  },
  replace(next: CartLine[]) {
    lines = next;
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } catch {
      // Storage unavailable (private mode, quota): keep the in-memory cart.
    }
    notify();
  }
};
