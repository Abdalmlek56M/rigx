"use client";

import { useSyncExternalStore } from "react";
import { getCatalogPending, subscribeCatalogPending } from "./catalog-pending";

/** True while the list on screen is waiting for another query of the same page (the cards then fade). */
export function useCatalogPending(): boolean {
  return useSyncExternalStore(subscribeCatalogPending, () => getCatalogPending() !== null, () => false);
}
