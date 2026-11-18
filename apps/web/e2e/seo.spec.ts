import { expect, test } from "@playwright/test";
import { getJson } from "./support/api";
import { SIGNED_OUT } from "./support/env";

interface StorePage {
  items: { code: string; slug: string; modelName: string }[];
}

test.use({ storageState: SIGNED_OUT });

test("old RX links move permanently to the readable product page, which search engines can read", async ({ page, request }) => {
  const { items } = await getJson<StorePage>(request, "/api/v1/store/items?pageSize=1");
  const item = items[0];
  if (!item) throw new Error("The demo store has no published part; reset the demo data.");

  const old = await request.get(`/products/${item.code}`, { maxRedirects: 0 });
  expect(old.status()).toBe(301);
  expect(new URL(old.headers()["location"] ?? "", "http://x").pathname).toBe(`/p/${item.slug}`);

  await page.goto(`/p/${item.slug}`);
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", new RegExp(`/p/${item.slug}$`));
  const structured = (await page.locator('script[type="application/ld+json"]').allTextContents()).join("\n");
  expect(structured).toContain('"@type":"Product"');
  expect(structured).toContain('"priceCurrency":"EGP"');
  expect(structured).toContain('"@type":"BreadcrumbList"');

  const sitemap = await (await request.get("/sitemap.xml")).text();
  expect(sitemap).toContain(`/p/${item.slug}</loc>`);
  const robots = await (await request.get("/robots.txt")).text();
  expect(robots).toContain("Disallow: /admin");
  expect(robots).toContain("Sitemap: ");
});
