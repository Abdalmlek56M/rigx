import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import type { CategoryNode } from "@/lib/categories";
import { toApiQuery, type CatalogState } from "@/features/catalog/catalog-query";
import { pickRelatedItems, RELATED_MIN } from "./related-items";

const STATE: CatalogState = {
  search: "", category: null, brand: null, condition: null, stock: "all", min: null, max: null, sort: "newest", page: 1, specs: {}
};

async function fetchItems(categoryId: string | null, showSoldOut: boolean): Promise<Schemas["StoreItemCard"][]> {
  try {
    const page = await required(
      unwrap(createServerApi().GET("/api/v1/store/items", { params: { query: toApiQuery(STATE, { categoryId, brandId: null }, showSoldOut) } }))
    );
    return page.items;
  } catch {
    return [];
  }
}

/** Same-category items first, then the parent category, then the newest items; empty when the API is unavailable. */
export async function loadRelatedItems(
  item: { code: string; categoryId: string | null },
  trail: CategoryNode[],
  showSoldOut: boolean
): Promise<Schemas["StoreItemCard"][]> {
  if (!item.categoryId) return pickRelatedItems(item.code, await fetchItems(null, showSoldOut));

  const same = await fetchItems(item.categoryId, showSoldOut);
  if (pickRelatedItems(item.code, same).length >= RELATED_MIN) return pickRelatedItems(item.code, same);

  const parentId = trail.at(-2)?.id ?? null;
  const parent = parentId ? await fetchItems(parentId, showSoldOut) : [];
  if (pickRelatedItems(item.code, same, parent).length >= RELATED_MIN) return pickRelatedItems(item.code, same, parent);

  const newest = await fetchItems(null, showSoldOut);
  return pickRelatedItems(item.code, same, parent, newest);
}
