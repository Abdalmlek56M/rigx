import type { MetadataRoute } from "next";
import { absoluteUrl, localizedPath } from "@/lib/seo/urls";

/** Private or per-customer areas (they are also `noindex`); listed for both locales. */
const PRIVATE_PATHS = ["/admin", "/account", "/cart", "/checkout", "/login", "/builder/quote", "/i/", "/t/"];

/**
 * One policy for every crawler, AI search and training bots included: the store wants its parts found and cited.
 * Product photos stay crawlable (`/api/v1/catalog/media/`); the rest of the API does not.
 */
export default function robots(): MetadataRoute.Robots {
  const disallow = ["/api/", ...PRIVATE_PATHS.flatMap((path) => [path, localizedPath("en", path)])];
  return {
    rules: [{ userAgent: "*", allow: ["/", "/api/v1/catalog/media/"], disallow }],
    sitemap: absoluteUrl("/sitemap.xml")
  };
}
