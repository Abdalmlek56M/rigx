import type { MetadataRoute } from "next";
import { siteUrl } from "@/lib/server-catalog";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: ["/", "/products", "/products/", "/categories"],
      disallow: ["/admin", "/admin/", "/account", "/account/", "/api/", "/login", "/register", "/verify-email", "/reset-password", "/forgot-password"],
    },
    sitemap: `${siteUrl()}/sitemap.xml`,
    host: siteUrl(),
  };
}
