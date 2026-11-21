/** A URL whose list navigation is still loading; the grid dims until the new results arrive. */
let pendingUrl: string | null = null;
const listeners = new Set<() => void>();

function notify(): void {
  listeners.forEach((listener) => listener());
}

export function markCatalogPending(url: string): void {
  pendingUrl = url;
  notify();
}

export function clearCatalogPending(): void {
  if (pendingUrl === null) return;
  pendingUrl = null;
  notify();
}

export function getCatalogPending(): string | null {
  return pendingUrl;
}

export function subscribeCatalogPending(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}
