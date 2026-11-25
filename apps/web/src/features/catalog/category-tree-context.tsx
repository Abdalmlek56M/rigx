"use client";

import { createContext, useContext, useMemo, type ReactNode } from "react";
import type { Schemas } from "@rigx/api-client";
import { effectiveSpecs, type CategoryNode } from "@/lib/categories";
import { cardSpecs, type SpecRow } from "@/features/product/spec-rows";

const CategoryTreeContext = createContext<CategoryNode[]>([]);
const BrandNamesContext = createContext<Readonly<Record<string, string>>>({});

/** The storefront's category tree and brand names for client parts that need them (catalog cards: spec names and units, the brand line). */
export function CategoryTreeProvider({ tree, brandNames, children }: { tree: CategoryNode[]; brandNames: Readonly<Record<string, string>>; children: ReactNode }) {
  return (
    <CategoryTreeContext value={tree}>
      <BrandNamesContext value={brandNames}>{children}</BrandNamesContext>
    </CategoryTreeContext>
  );
}

/** The name of an item's brand, or null when it has none (or the brand is no longer active). */
export function useBrandName(brandId: string | null | undefined): string | null {
  const names = useContext(BrandNamesContext);
  return brandId ? (names[brandId] ?? null) : null;
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
