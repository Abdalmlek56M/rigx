import type { MetadataRoute } from "next";
import { unwrap, type Schemas } from "@rigx/api-client";
import { categoryHref } from "@/components/layout/nav-links";
import { publishedPolicySlugs } from "@/features/policies/published-policies";
import { createServerApi } from "@/lib/api/server";
import { indexedLocales } from "@/lib/seo/page-seo";
import { absoluteUrl, languageAlternates, localizedPath, productPath } from "@/lib/seo/urls";
import { loadStorefront } from "@/lib/storefront";
import type { CategoryNode } from "@/lib/categories";

/** Rebuilt at most hourly, so new items, builds and categories reach the sitemap without a deploy. */
export const revalidate = 3600;

const STATIC_PATHS = ["/", "/products", "/builds", "/builder", "/services", "/faq", "/warranty"];
const API_PAGE_SIZE = 60;
/** Safety stop for the item paging (60 × 200 = 12,000 items, far under the 50,000 URL limit). */
const MAX_PAGES = 200;

async function itemSlugs(): Promise<string[]> {
  const api = createServerApi();
  const slugs: string[] = [];
  for (let page = 1; page <= MAX_PAGES; page++) {
    const result: Schemas["StoreItemPage"] | undefined = await unwrap(api.GET("/api/v1/store/items", { params: { query: { page, pageSize: API_PAGE_SIZE } } }));
    if (!result) break;
    slugs.push(...result.items.map((item) => item.slug));
    if (page * API_PAGE_SIZE >= result.total) break;
  }
  return slugs;
}

async function buildCodes(): Promise<string[]> {
  return ((await unwrap(createServerApi().GET("/api/v1/store/assemblies"))) ?? []).map((build) => build.code);
}

async function policyPaths(): Promise<string[]> {
  return (await publishedPolicySlugs()).map((slug) => `/policies/${slug}`);
}

function categoryPaths(nodes: CategoryNode[]): string[] {
  return nodes.flatMap((node) => [categoryHref(node.slug), ...categoryPaths(node.children)]);
}

/** A failing source only drops its own URLs (the rest of the sitemap is still served). */
async function settle(source: Promise<string[]>, name: string): Promise<string[]> {
  try {
    return await source;
  } catch (error) {
    console.warn(`Sitemap: ${name} unavailable:`, error instanceof Error ? error.message : error);
    return [];
  }
}

/** Every indexable public page in every indexed locale, each with its hreflang alternates. */
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [storefront, locales, items, builds, policies] = await Promise.all([
    loadStorefront(),
    indexedLocales(),
    settle(itemSlugs(), "items"),
    settle(buildCodes(), "builds"),
    settle(policyPaths(), "policies")
  ]);
  const paths = [
    ...STATIC_PATHS,
    ...categoryPaths(storefront.categoryTree),
    ...policies,
    ...builds.map((code) => `/builds/${code}`),
    ...items.map(productPath)
  ];
  return paths.flatMap((path) =>
    locales.map((locale) => ({
      url: absoluteUrl(localizedPath(locale, path)),
      ...(locales.length > 1 ? { alternates: { languages: languageAlternates(path, locales) } } : {})
    }))
  );
}
