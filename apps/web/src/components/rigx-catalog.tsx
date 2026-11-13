"use client";

import { useEffect, useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  getCatalogBrands,
  getCatalogCategories,
  getCatalogProducts,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProductPage,
} from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { RigxProductCard } from "@/components/rigx-product-card";
import { LoadingCard } from "@/components/ui";

const sortOptions = [
  { value: "featured", ar: "المميز أولًا", en: "Featured first" },
  { value: "newest", ar: "الأحدث", en: "Newest" },
  { value: "price-asc", ar: "السعر: من الأقل", en: "Price: low to high" },
  { value: "price-desc", ar: "السعر: من الأعلى", en: "Price: high to low" },
  { value: "name-asc", ar: "الاسم: أ - ي", en: "Name: A–Z" },
  { value: "name-desc", ar: "الاسم: ي - أ", en: "Name: Z–A" },
] as const;

export function RigxCatalog() {
  const { locale } = useLocale();
  const isArabic = locale === "ar";
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();

  const initialCategory = params.get("category") ?? "";
  const initialBrand = params.get("brand") ?? "";
  const initialQuery = params.get("q") ?? "";

  const [term, setTerm] = useState(initialQuery);
  const [query, setQuery] = useState(initialQuery);
  const [category, setCategory] = useState(initialCategory);
  const [brand, setBrand] = useState(initialBrand);
  const [sort, setSort] = useState<string>("featured");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [page, setPage] = useState(1);
  const [data, setData] = useState<CatalogProductPage | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void Promise.allSettled([getCatalogCategories(), getCatalogBrands()]).then(([categoryResult, brandResult]) => {
      if (categoryResult.status === "fulfilled") setCategories(categoryResult.value.filter((item) => item.isActive && item.isEffectivelyActive));
      if (brandResult.status === "fulfilled") setBrands(brandResult.value.filter((item) => item.isActive));
    });
  }, []);

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setQuery(term.trim());
      setPage(1);
    }, 280);
    return () => window.clearTimeout(handle);
  }, [term]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void getCatalogProducts({
      search: query || undefined,
      category: category || undefined,
      brand: brand || undefined,
      sort: sort === "featured" ? undefined : sort as never,
      page,
      pageSize: 12,
    })
      .then((result) => { if (!cancelled) setData(result); })
      .catch(() => { if (!cancelled) setData(null); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [query, category, brand, sort, page]);

  useEffect(() => {
    const next = new URLSearchParams();
    if (query) next.set("q", query);
    if (category) next.set("category", category);
    if (brand) next.set("brand", brand);
    const search = next.toString();
    router.replace(`${pathname}${search ? `?${search}` : ""}`, { scroll: false });
  }, [query, category, brand, pathname, router]);

  const totalPages = useMemo(() => {
    if (!data) return 1;
    return Math.max(1, Math.ceil(data.totalCount / data.pageSize));
  }, [data]);

  const activeFilters = [category, brand].filter(Boolean).length;

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">HARDWARE CATALOG / 01</span>
          <h1>{isArabic ? <>كتالوج <em>المكوّنات.</em></> : <>Component <em>catalog.</em></>}</h1>
          <p>{isArabic ? "ابحث في منتجات RIGX المنشورة، وقارن المواصفات والسعر من بيانات الكتالوج الفعلية." : "Search published RIGX products and compare specifications and prices from the live catalog."}</p>
        </div>
        <div className="hero-side-stats">
          <span><b>{data ? String(data.totalCount).padStart(2, "0") : "—"}</b><small>{isArabic ? "منتج متاح" : "products"}</small></span>
          <span><b>{String(categories.length).padStart(2, "0")}</b><small>{isArabic ? "قسم رئيسي" : "departments"}</small></span>
        </div>
      </div>

      <div className="catalog-toolbar glass-panel">
        <label className="catalog-search">
          <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10.8" cy="10.8" r="6.8" /><path d="m16 16 5 5" /></svg>
          <input value={term} onChange={(event) => setTerm(event.target.value)} placeholder={isArabic ? "ابحث بالاسم أو SKU أو الماركة" : "Search by name, SKU or brand"} />
        </label>
        <select value={sort} onChange={(event) => { setSort(event.target.value); setPage(1); }} aria-label={isArabic ? "ترتيب المنتجات" : "Sort products"}>
          {sortOptions.map((option) => <option key={option.value} value={option.value}>{isArabic ? option.ar : option.en}</option>)}
        </select>
        <button className="filter-toggle" type="button" onClick={() => setFiltersOpen((open) => !open)}>
          {isArabic ? "الفلاتر" : "Filters"} {activeFilters > 0 ? `(${activeFilters})` : ""}
        </button>
      </div>

      <div className="catalog-layout">
        <aside className={`filter-panel glass-panel${filtersOpen ? " open" : ""}`}>
          <div className="filter-head">
            <b>{isArabic ? "تصفية النتائج" : "Filter results"}</b>
            <button type="button" onClick={() => { setCategory(""); setBrand(""); setPage(1); }}>{isArabic ? "مسح الكل" : "Clear all"}</button>
          </div>
          <div className="filter-group">
            <b>{isArabic ? "القسم" : "Department"}</b>
            {categories.map((item) => (
              <label key={item.id}>
                <input type="radio" name="category" checked={category === item.slug} onChange={() => { setCategory(item.slug); setPage(1); }} />
                {localizedCategoryName(item.slug, item.name, locale)}
              </label>
            ))}
          </div>
          {brands.length > 0 ? (
            <div className="filter-group">
              <b>{isArabic ? "الماركة" : "Brand"}</b>
              {brands.slice(0, 10).map((item) => (
                <label key={item.id}>
                  <input type="radio" name="brand" checked={brand === item.slug} onChange={() => { setBrand(item.slug); setPage(1); }} />
                  {item.name}
                </label>
              ))}
            </div>
          ) : null}
          <div className="filter-note">
            {isArabic ? "الأسعار والمخزون يقرأان مباشرة من الخادم، ولا تُعرض بيانات غير منشورة." : "Prices and availability are read directly from the server; unpublished data is never shown."}
          </div>
        </aside>

        <div className="catalog-results">
          <div className="results-head">
            <span>{data ? (isArabic ? `${data.totalCount} نتيجة` : `${data.totalCount} results`) : (isArabic ? "جارٍ التحميل..." : "Loading...")}</span>
            <div className="active-chips">
              {category ? <span>{localizedCategoryName(category, category, locale)}</span> : null}
              {brand ? <span>{brands.find((item) => item.slug === brand)?.name ?? brand}</span> : null}
            </div>
          </div>
          {loading ? <LoadingCard /> : !data || data.items.length === 0 ? (
            <div className="order-empty compact-empty">
              <div className="empty-icon">+</div>
              <h3>{isArabic ? "لا نتائج مطابقة" : "No matching results"}</h3>
              <p>{isArabic ? "جرّب تعديل البحث أو إزالة بعض الفلاتر." : "Try a different search or remove some filters."}</p>
            </div>
          ) : (
            <div className="product-grid catalog-grid">
              {data.items.map((product) => <RigxProductCard key={product.id} product={product} />)}
            </div>
          )}
          {totalPages > 1 ? (
            <nav className="rigx-pagination" aria-label={isArabic ? "صفحات النتائج" : "Result pages"}>
              <button type="button" className="btn-outline" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>{isArabic ? "السابق" : "Previous"}</button>
              <span>{page} / {totalPages}</span>
              <button type="button" className="btn-outline" disabled={page >= totalPages} onClick={() => setPage((value) => Math.min(totalPages, value + 1))}>{isArabic ? "التالي" : "Next"}</button>
            </nav>
          ) : null}
        </div>
      </div>
    </>
  );
}

