/** One stock item as the item search shows it: what a person recognises it by (its picture, its name) and the code that is stored. */
import type { Schemas } from "@rigx/api-client";

export interface ItemHit {
  code: string;
  name: string;
  thumbnailStorageKey: string | null;
}

/** The shortest text that is searched (one letter would list the whole stock). */
export const ITEM_SEARCH_MIN = 2;
/** How many matches the list shows. */
export const ITEM_SEARCH_SIZE = 8;

/** A hit from the admin stock list (its first photo) or from the public store list (its card thumbnail). */
export function toItemHit(row: Schemas["StockItemAdminView"] | Schemas["StoreItemCard"]): ItemHit {
  const thumbnail = "photos" in row ? row.photos[0]?.thumbnailStorageKey : row.thumbnailStorageKey;
  return { code: row.code, name: row.modelName, thumbnailStorageKey: thumbnail ?? null };
}

/** The text is long enough to search. */
export const searchable = (text: string): boolean => text.trim().length >= ITEM_SEARCH_MIN;

/** The hit whose code is exactly `code` (the item behind a stored code), ignoring case. */
export function hitForCode(hits: readonly ItemHit[], code: string): ItemHit | null {
  const wanted = code.trim().toUpperCase();
  return hits.find((hit) => hit.code.toUpperCase() === wanted) ?? null;
}
