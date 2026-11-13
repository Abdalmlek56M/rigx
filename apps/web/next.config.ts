import type { NextConfig } from "next";
import { PHASE_DEVELOPMENT_SERVER } from "next/constants";

export default (phase: string): NextConfig => ({
  output: "standalone",
  allowedDevOrigins: ["127.0.0.1"],
  devIndicators: false,
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
        ],
      },
    ];
  },
  async rewrites() {
    if (phase !== PHASE_DEVELOPMENT_SERVER) return [];

    const apiOrigin = process.env.API_ORIGIN ?? "http://localhost:8080";
    return [{ source: "/api/:path*", destination: `${apiOrigin}/api/:path*` }];
  },
});
