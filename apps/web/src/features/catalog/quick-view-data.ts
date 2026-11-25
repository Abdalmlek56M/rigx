"use server";

import { getTranslations } from "next-intl/server";
import { effectiveSpecs } from "@/lib/categories";
import { loadStorefront } from "@/lib/storefront";
import { loadItem } from "@/features/product/load-item";
import { specRows, type SpecRow } from "@/features/product/spec-rows";

/** The most specs the quick view lists; the product page has the full table. */
const MAX_SPECS = 6;

export interface QuickViewData {
  photos: { storageKey: string; thumbnailStorageKey: string }[];
  brand: string | null;
  warrantyLabel: string | null;
  specs: SpecRow[];
}

/** What the quick view shows beyond the card: the photos, brand, warranty and the first specs. `null` when the item is gone. */
export async function loadQuickView(slug: string): Promise<QuickViewData | null> {
  const [item, storefront, t] = await Promise.all([loadItem(slug), loadStorefront(), getTranslations("product.specValue")]);
  if (!item) return null;
  const defs = item.categoryId ? effectiveSpecs(storefront.categoryTree, item.categoryId) : [];
  const brand = item.brandId ? storefront.brands.find((entry) => entry.id === item.brandId) : undefined;
  return {
    photos: item.photos.map((photo) => ({ storageKey: photo.storageKey, thumbnailStorageKey: photo.thumbnailStorageKey })),
    brand: brand?.name ?? null,
    warrantyLabel: item.warrantyLabel,
    specs: specRows(item.specs, defs, { yes: t("yes"), no: t("no") }).slice(0, MAX_SPECS)
  };
}
