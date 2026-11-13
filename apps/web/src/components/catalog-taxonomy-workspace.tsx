"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  getAdminBrands,
  getAdminCategories,
  getAdminProducts,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { CatalogTaxonomyAdmin } from "@/components/catalog-taxonomy-admin";
import { LoadingCard, Message, PageIntro } from "@/components/ui";

export function CatalogTaxonomyWorkspace({ initialTab }: { initialTab: "category" | "brand" }) {
  const { messages } = useLocale();
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  async function load(showLoading = true) {
    if (showLoading) setLoading(true);
    setError(null);
    try {
      const [categoryData, brandData, productData] = await Promise.all([
        getAdminCategories(),
        getAdminBrands(),
        getAdminProducts(),
      ]);
      setCategories(categoryData);
      setBrands(brandData);
      setProducts(productData);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      if (showLoading) setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) return <LoadingCard />;

  return (
    <main className="p-4 sm:p-6 lg:p-8">
      <div className="mx-auto max-w-[80rem]">
        <nav aria-label={messages.adminGroupCatalog} className="mb-4 flex flex-wrap items-center gap-2 text-sm font-semibold text-[var(--ds-text-muted)]">
          <Link href="/admin" className="hover:text-[var(--ds-accent)]">{messages.adminDashboardNav}</Link>
          <span aria-hidden="true">/</span>
          <Link href="/admin/catalog" className="hover:text-[var(--ds-accent)]">{messages.adminCatalogWorkspace}</Link>
          <span aria-hidden="true">/</span>
          <span aria-current="page" className="text-[var(--ds-text)]">
            {initialTab === "brand" ? messages.adminBrandsNav : messages.adminCategoriesNav}
          </span>
        </nav>
        <PageIntro
          eyebrow="RIGX / CATALOG"
          title={initialTab === "brand" ? messages.adminBrandsNav : messages.adminCategoriesNav}
          lead={messages.catalogCategoriesLead}
        />
        {notice ? <div className="my-4"><Message tone="success">{notice}</Message></div> : null}
        {error ? <div className="my-4"><Message tone="error">{error}</Message></div> : null}
        <CatalogTaxonomyAdmin
          categories={categories}
          brands={brands}
          products={products}
          initialTab={initialTab}
          onChanged={() => load(false)}
          onNotice={setNotice}
          onError={setError}
        />
      </div>
    </main>
  );
}
