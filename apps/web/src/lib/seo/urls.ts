/** Arabic is the default locale and has no URL prefix (`localePrefix: "as-needed"`). */
const DEFAULT_LOCALE = "ar";
const DEV_ORIGIN = "http://localhost:3000";

/** The public origin from `NEXT_PUBLIC_SITE_URL` (production: https://rigxhardware.com), without a trailing slash. */
export function siteOrigin(raw: string | undefined = process.env.NEXT_PUBLIC_SITE_URL): string {
  return (raw?.trim() || DEV_ORIGIN).replace(/\/+$/, "");
}

/** A storefront path in a locale: Arabic as is, English under `/en` (`/` becomes `/en`). */
export function localizedPath(locale: string, path: string): string {
  if (locale === DEFAULT_LOCALE) return path;
  return path === "/" ? `/${locale}` : `/${locale}${path}`;
}

export function absoluteUrl(path: string, origin: string = siteOrigin()): string {
  return `${origin}${path}`;
}

/** hreflang map for a path: every listed locale plus `x-default` (the Arabic page). */
export function languageAlternates(path: string, locales: readonly string[], origin: string = siteOrigin()): Record<string, string> {
  const languages: Record<string, string> = {};
  for (const locale of locales) languages[locale] = absoluteUrl(localizedPath(locale, path), origin);
  languages["x-default"] = absoluteUrl(localizedPath(DEFAULT_LOCALE, path), origin);
  return languages;
}

/** The product page path (owner decision 18 Nov 2026: readable slug, no RX code). */
export function productPath(slug: string): string {
  return `/p/${slug}`;
}

const LEGACY_PRODUCT = /^\/(?:(ar|en)\/)?products\/(rx-?\d+)\/?$/i;

/** An old `/products/RX-0142` link (any locale prefix), which now redirects (301) to the product's slug. */
export function parseLegacyProductPath(pathname: string): { locale: string; code: string } | null {
  const match = LEGACY_PRODUCT.exec(pathname);
  return match ? { locale: match[1]?.toLowerCase() ?? DEFAULT_LOCALE, code: match[2] ?? "" } : null;
}
