import type { Schemas } from "@rigx/api-client";

type RelatedItem = Schemas["StoreItemCard"];

/** The page shows at least this many related items when it can, and never more than `RELATED_MAX`. */
export const RELATED_MIN = 6;
export const RELATED_MAX = 12;

/** In stock first, keeping the API's newest-first order inside each group. */
function availableFirst(items: readonly RelatedItem[]): RelatedItem[] {
  return [...items.filter((item) => item.available > 0), ...items.filter((item) => item.available <= 0)];
}

/**
 * The "more like this" list: same category first, then the parent category when the list is short, then the newest
 * items. Items are de-duplicated by code and the current item is always left out.
 */
export function pickRelatedItems(
  currentCode: string,
  sameCategory: readonly RelatedItem[],
  parentCategory: readonly RelatedItem[] = [],
  newest: readonly RelatedItem[] = []
): RelatedItem[] {
  const seen = new Set([currentCode]);
  const add = (items: readonly RelatedItem[]): RelatedItem[] => {
    const added: RelatedItem[] = [];
    for (const item of availableFirst(items)) {
      if (seen.has(item.code)) continue;
      seen.add(item.code);
      added.push(item);
    }
    return added;
  };

  const same = add(sameCategory);
  if (same.length >= RELATED_MIN) return same.slice(0, RELATED_MAX);

  const withParent = [...same, ...add(parentCategory)];
  if (withParent.length >= RELATED_MIN) return withParent.slice(0, RELATED_MAX);

  return [...withParent, ...add(newest)].slice(0, RELATED_MAX);
}
