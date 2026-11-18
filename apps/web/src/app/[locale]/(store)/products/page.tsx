import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { hasLocale } from "next-intl";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { unwrap, type Schemas } from "@rigx/api-client";
import { CatalogHeader } from "@/features/catalog/CatalogHeader";
import { CatalogToolbar } from "@/features/catalog/CatalogToolbar";
import { FilterPanel } from "@/features/catalog/FilterPanel";
import { ItemCard } from "@/features/catalog/ItemCard";
import { Pagination } from "@/features/catalog/Pagination";
import { RequestPartBanner } from "@/features/catalog/RequestPartBanner";
import { catalogTitle } from "@/features/catalog/catalog-title";
import { EmptyIcon } from "@/components/ui/EmptyIcon";
import { SearchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { catalogCanonicalPath, catalogHref, pageCount, parseCatalogParams, toApiQuery, toSearchParams, type RawParams } from "@/features/catalog/catalog-query";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import { categoryBySlug } from "@/lib/categories";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";
import { pageSeo } from "@/lib/seo/page-seo";

interface Props {
  params: Promise<{ locale: string }>;
  searchParams: Promise<RawParams>;
}

/** The catalog state from the URL; an unknown category slug is ignored (with its spec filters). */
async function resolve(searchParams: Promise<RawParams>) {
  const storefront = await loadStorefront();
  const parsed = parseCatalogParams(await searchParams);
  const category = parsed.category ? categoryBySlug(storefront.categoryTree, parsed.category) : undefined;
  const state = parsed.category && !category ? { ...parsed, category: null, specs: {} } : parsed;
  return { storefront, state, category };
}

export async function generateMetadata({ params, searchParams }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) return {};
  setRequestLocale(locale);
  const { state, category } = await resolve(searchParams);
  const seo = await pageSeo(locale, catalogCanonicalPath(state));
  // Search results and filtered views point at their category page; search results stay out of the index.
  return { title: await catalogTitle(state, category), ...seo, ...(state.search ? { robots: { index: false, follow: true } } : {}) };
}

async function loadItems(query: ReturnType<typeof toApiQuery>): Promise<Schemas["StoreItemPage"] | null> {
  try {
    return await required(unwrap(createServerApi().GET("/api/v1/store/items", { params: { query } })));
  } catch (error) {
    console.warn("Catalog items unavailable:", error instanceof Error ? error.message : error);
    return null;
  }
}

export default async function ProductsPage({ params, searchParams }: Props) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const t = await getTranslations("catalog");
  const { storefront, state, category } = await resolve(searchParams);
  const result = await loadItems(toApiQuery(state, category?.id ?? null, storefront.showSoldOut));
  const queryKey = toSearchParams({ ...state, page: 1 }).toString();
  const panel = (idPrefix: string) => (
    <FilterPanel state={state} tree={storefront.categoryTree} categoryId={category?.id ?? null} showSoldOut={storefront.showSoldOut} idPrefix={idPrefix} />
  );

  return (
    <>
      <CatalogHeader
        state={state}
        tree={storefront.categoryTree}
        category={category}
        title={await catalogTitle(state, category)}
        counts={result?.counts ?? { new: 0, used: 0 }}
      />
      <section className="mx-auto flex max-w-[1240px] items-start gap-7 px-4 pb-16 pt-2 md:px-6">
        <aside aria-label={t("filters.panelLabel")} className="hidden w-[280px] shrink-0 rounded-[10px] border border-line bg-surface p-5 md:block">
          {panel("side")}
        </aside>
        <div className="flex min-w-0 flex-1 flex-col gap-4">
          <CatalogToolbar state={state} tree={storefront.categoryTree} total={result?.total ?? 0} panel={panel("sheet")} queryKey={queryKey} />
          {result === null ? (
            <p role="alert" className="rounded-card border border-line bg-surface p-6 text-center">{t("loadFailed")}</p>
          ) : result.items.length === 0 ? (
            <div className="flex flex-col items-center gap-3 rounded-card border border-line bg-surface p-10 text-center">
              <EmptyIcon icon={SearchIcon} />
              <b className="text-lg">{t("empty.title")}</b>
              <p className="m-0 text-ink-muted">{t("empty.body")}</p>
              <Link href="/products" className="font-bold text-orange-text underline hover:text-orange-text-hover">{t("empty.clear")}</Link>
            </div>
          ) : (
            <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(170px,1fr))] gap-3 p-0 md:grid-cols-[repeat(auto-fill,minmax(250px,1fr))] md:gap-4">
              {result.items.map((item) => (
                <li key={item.code} className="flex">
                  <ItemCard item={item} />
                </li>
              ))}
            </ul>
          )}
          <Pagination current={state.page} pages={pageCount(result?.total ?? 0)} hrefFor={(page) => catalogHref(state, { page })} />
          <RequestPartBanner />
        </div>
      </section>
    </>
  );
}
