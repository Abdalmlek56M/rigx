import createMiddleware from "next-intl/middleware";
import { NextResponse, type NextRequest } from "next/server";
import { routing } from "./i18n/routing";
import { createServerApi } from "./lib/api/server";
import { localizedPath, parseLegacyProductPath, productPath } from "./lib/seo/urls";

const handleI18nRouting = createMiddleware(routing);
const MOVED_PERMANENTLY = 301;

/** The current slug of a published item by its old RX code; `null` when it is unknown, hidden or the API fails. */
async function slugForCode(code: string): Promise<string | null> {
  try {
    const { data } = await createServerApi({ fresh: true }).GET("/api/v1/store/items/{key}", { params: { path: { key: code } } });
    return data?.slug ?? null;
  } catch {
    return null;
  }
}

/** Old `/products/RX-0142` links answer 301 to `/p/<slug>`; everything else goes through the locale routing. */
export default async function proxy(request: NextRequest) {
  const legacy = parseLegacyProductPath(request.nextUrl.pathname);
  const slug = legacy ? await slugForCode(legacy.code) : null;
  if (legacy && slug) return NextResponse.redirect(new URL(localizedPath(legacy.locale, productPath(slug)), request.url), MOVED_PERMANENTLY);
  return handleI18nRouting(request);
}

export const config = {
  // Everything except the API, Next internals, the share image and files with an extension.
  matcher: ["/((?!api|_next|_vercel|opengraph-image|.*\\..*).*)"]
};
