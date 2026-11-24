/** The tag on every cached storefront response about items, builds and categories (what a visitor browses); an admin change expires it at once. */
export const CATALOG_TAG = "catalog";

const CATALOG_PATH = /\/api\/v1\/(store|catalog)\//;

/** The cache tags a server-side API call gets, from its address: catalog data carries `CATALOG_TAG`, everything else none. */
export function cacheTagsFor(url: string): string[] {
  return CATALOG_PATH.test(url) ? [CATALOG_TAG] : [];
}
