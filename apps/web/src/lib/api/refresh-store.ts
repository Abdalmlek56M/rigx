"use server";

import { cookies } from "next/headers";
import { updateTag } from "next/cache";
import { unwrap } from "@rigx/api-client";
import { CATALOG_TAG } from "./cache-tags";
import { createServerApi } from "./server";

/** The permission that may change what the store lists (hide, show, edit, photos); the API checks it on every change, this checks it before a cache is cleared. */
const STOCK_ADJUST = "Inventory.StockAdjust";

/**
 * Expires the cached catalog so a hide, a show or an edit reaches visitors at once instead of after the cache's five minutes. Only a
 * signed-in person with `Inventory.StockAdjust` (asked of the API with their own cookie) can do it; anyone else changes nothing.
 */
export async function refreshCatalogCache(): Promise<boolean> {
  const cookie = (await cookies()).toString();
  if (!cookie) return false;
  try {
    const me = await unwrap(createServerApi({ cookie }).GET("/api/v1/identity/me"));
    if (!me?.permissions.includes(STOCK_ADJUST)) return false;
  } catch {
    return false;
  }
  updateTag(CATALOG_TAG);
  return true;
}
