"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { suggestBrandsFirst } from "../brands/brand-rules";
import { createBrand, type Brand } from "../brands/brands-api";
import { findPath } from "../categories/category-rules";
import type { Category } from "../categories/categories-api";
import { ComboBox } from "../kit/ComboBox";
import { usePermissions } from "../use-permissions";
import { brandSlug } from "./brand-slug";

/**
 * The searchable brand picker of the stock forms; staff who may manage the catalog can add a missing brand inline. Once the item's
 * category is known (`tree` and `categoryId`) the brands suggested for it come first, marked; every other brand stays in the list.
 */
export function BrandSelect({ brands, tree, categoryId, value, onChange, onCreated, name = "brandId", error }: {
  brands: readonly Brand[];
  tree?: readonly Category[];
  categoryId?: string;
  value: string;
  onChange: (id: string) => void;
  /** Lets the screen keep the new brand in its own lookup list too (the line cards remount on validation). */
  onCreated?: (brand: Brand) => void;
  name?: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.inventory.fields");
  const tProblem = useTranslations("admin.purchases.brands");
  const { has } = usePermissions();
  const [created, setCreated] = useState<Brand[]>([]);
  const [problem, setProblem] = useState<string | null>(null);
  const canCreate = has("Catalog.ManageTaxonomy");
  const all = [...brands, ...created.filter((brand) => !brands.some((known) => known.id === brand.id))];
  const chain = tree && categoryId ? findPath(tree, categoryId).map((category) => category.id) : [];

  async function create(name: string) {
    const clean = name.trim();
    const existing = all.find((brand) => brand.name.trim().toLocaleLowerCase() === clean.toLocaleLowerCase());
    if (existing) {
      setProblem(null);
      onChange(existing.id);
      return;
    }
    const slug = brandSlug(clean);
    if (!slug) {
      setProblem(tProblem("englishName"));
      return;
    }
    setProblem(null);
    try {
      const brand = await createBrand({
        name: clean, slug, description: null, logoStorageKey: null, supportUrl: null, isActive: true, rowVersion: null
      });
      setCreated((current) => [...current, brand]);
      onCreated?.(brand);
      onChange(brand.id);
    } catch {
      setProblem(tProblem("createFailed"));
    }
  }

  function select(id: string) {
    setProblem(null);
    onChange(id);
  }

  return (
    <ComboBox
      label={t("brand")} name={name} value={value} error={error ?? problem}
      options={[
        { value: "", label: t("noBrand") },
        ...suggestBrandsFirst(all, chain).map(({ brand, suggested }) => ({ value: brand.id, label: brand.name, note: suggested ? t("suggested") : undefined }))
      ]}
      onChange={select} onCreate={canCreate ? create : undefined}
    />
  );
}
