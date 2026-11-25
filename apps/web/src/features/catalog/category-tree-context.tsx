"use client";

import { createContext, useContext, useMemo, type ReactNode } from "react";
import type { Schemas } from "@rigx/api-client";
import { effectiveSpecs, type CategoryNode } from "@/lib/categories";
import { cardSpecs, type SpecRow } from "@/features/product/spec-rows";

const CategoryTreeContext = createContext<CategoryNode[]>([]);

/** The storefront's category tree for client parts that need spec names and units (catalog cards). */
export function CategoryTreeProvider({ tree, children }: { tree: CategoryNode[]; children: ReactNode }) {
  return <CategoryTreeContext value={tree}>{children}</CategoryTreeContext>;
}

/** The two or three key facts a card shows for an item (see `cardSpecs`). */
export function useCardSpecs(item: Pick<Schemas["StoreItemCard"], "categoryId" | "specs">): SpecRow[] {
  const tree = useContext(CategoryTreeContext);
  return useMemo(
    // A card cached before the API sent specs has none; it simply shows no facts until the cache refreshes.
    () => (item.categoryId && item.specs ? cardSpecs(item.specs, effectiveSpecs(tree, item.categoryId)) : []),
    [tree, item.categoryId, item.specs]
  );
}
