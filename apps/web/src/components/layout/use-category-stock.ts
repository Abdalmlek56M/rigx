"use client";

import { useEffect, useState } from "react";
import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";

/** Newest items shown in the mega menu for the hovered category. */
const FEATURED_COUNT = 3;

export interface CategoryStock {
  items: Schemas["StoreItemCard"][];
  counts: Schemas["StoreConditionCounts"];
}

/** One request per category per page view; a failed lookup is dropped so the next open tries again. */
const cache = new Map<string, Promise<CategoryStock | null>>();

function load(categoryId: string): Promise<CategoryStock | null> {
  let pending = cache.get(categoryId);
  if (!pending) {
    pending = unwrap(
      browserApi().GET("/api/v1/store/items", { params: { query: { categoryId, inStock: true, sort: "newest", pageSize: FEATURED_COUNT } } })
    ).then(
      (page) => (page ? { items: page.items, counts: page.counts } : null),
      () => null
    );
    pending.then((stock) => {
      if (!stock) cache.delete(categoryId);
    });
    cache.set(categoryId, pending);
  }
  return pending;
}

/**
 * The available stock of a category (its newest items and new/used counts), fetched only while the menu is open.
 * `undefined` while loading, `null` when the lookup failed.
 */
export function useCategoryStock(categoryId: string, enabled: boolean): CategoryStock | null | undefined {
  const [result, setResult] = useState<{ id: string; stock: CategoryStock | null } | null>(null);

  useEffect(() => {
    if (!enabled) return;
    let live = true;
    load(categoryId).then((stock) => {
      if (live) setResult({ id: categoryId, stock });
    });
    return () => {
      live = false;
    };
  }, [categoryId, enabled]);

  return result?.id === categoryId ? result.stock : undefined;
}
