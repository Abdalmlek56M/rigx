import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const apiOrigin = process.env.RIGX_API_ORIGIN ?? "http://localhost:8080";

const config: NextConfig = {
  transpilePackages: ["@rigx/api-client"],
  // One origin for the browser: the API cookies are __Host- prefixed and paired with a CSRF token.
  rewrites: async () => [{ source: "/api/:path*", destination: `${apiOrigin}/api/:path*` }]
};

export default createNextIntlPlugin("./src/i18n/request.ts")(config);
