import path from "node:path";
import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";
import { securityHeaders } from "./src/lib/security/csp";

const apiOrigin = process.env.RIGX_API_ORIGIN ?? "http://localhost:8080";
const production = process.env.NODE_ENV === "production";

const config: NextConfig = {
  transpilePackages: ["@rigx/api-client"],
  // Production image (apps/web/Dockerfile): a self-contained server that also traces the workspace packages.
  output: "standalone",
  outputFileTracingRoot: path.join(import.meta.dirname, "../.."),
  // One origin for the browser: the API cookies are __Host- prefixed and paired with a CSRF token.
  // In production host nginx sends /api straight to the API; this rewrite serves `next dev` and `next start`.
  rewrites: async () => [{ source: "/api/:path*", destination: `${apiOrigin}/api/:path*` }],
  // `next dev` needs eval and websockets, so the CSP is for production builds only.
  headers: async () =>
    production
      ? [
          {
            source: "/:path*",
            headers: securityHeaders({
              sentryDsn: process.env.NEXT_PUBLIC_SENTRY_DSN,
              gaId: process.env.NEXT_PUBLIC_GA_ID,
              metaPixelId: process.env.NEXT_PUBLIC_META_PIXEL_ID
            })
          }
        ]
      : []
};

export default createNextIntlPlugin("./src/i18n/request.ts")(config);
