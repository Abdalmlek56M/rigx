import "server-only";
import type { Metadata } from "next";
import { routing } from "@/i18n/routing";
import { loadStorefront } from "@/lib/storefront";
import { absoluteUrl, languageAlternates, localizedPath, siteOrigin } from "./urls";

/** The locales search engines should see: English only while it is switched on in settings. */
export async function indexedLocales(): Promise<readonly string[]> {
  const { englishEnabled } = await loadStorefront();
  return englishEnabled ? routing.locales : [routing.defaultLocale];
}

/**
 * Canonical URL and hreflang alternates of a public page (`path` without the locale prefix, query included when it is
 * part of the page's identity). An English page while English is off is kept out of the index instead.
 */
export async function pageSeo(locale: string, path: string): Promise<Pick<Metadata, "alternates" | "robots">> {
  const locales = await indexedLocales();
  const canonical = absoluteUrl(localizedPath(locale, path));
  if (!locales.includes(locale)) return { alternates: { canonical }, robots: { index: false, follow: true } };
  return { alternates: { canonical, ...(locales.length > 1 ? { languages: languageAlternates(path, locales) } : {}) } };
}

/** `@id` of the store's Organization node, so product offers can point at the seller. */
export function organizationId(): string {
  return `${siteOrigin()}/#organization`;
}

/**
 * Open Graph fields every page shares, with the default share image (`app/opengraph-image.tsx`, outside the locale
 * routing). A page that sets its own `openGraph` spreads these first (metadata merges shallowly) and may replace `images`.
 */
export function openGraphBase(locale: string) {
  return {
    siteName: "RIGX Hardware",
    locale: locale === "en" ? "en_US" : "ar_EG",
    type: "website" as const,
    images: [{ url: "/opengraph-image", width: 1200, height: 630, alt: "RIGX Hardware" }]
  };
}
