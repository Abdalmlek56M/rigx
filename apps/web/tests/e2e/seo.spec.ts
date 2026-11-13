import { expect, test } from "@playwright/test";

test.skip(!process.env.API_ORIGIN, "SEO E2E requires the isolated Preview API.");

test("product HTML contains truthful server-rendered metadata and structured data", async ({ request }) => {
  const response = await request.get("/products/amd-radeon-rx-9070-xt-16gb");
  expect(response.ok()).toBe(true);
  const html = await response.text();

  expect(html).toContain("<title>AMD Radeon RX 9070 XT 16GB | RIGX</title>");
  expect(html).toContain('property="og:title"');
  expect(html).toContain('rel="canonical"');
  expect(html).toContain('type="application/ld+json"');
  expect(html).toContain('"@type":"Product"');
  expect(html).toContain('"priceCurrency":"EGP"');
  expect(html).toContain("AMD Radeon RX 9070 XT 16GB");
});

test("robots and sitemap expose public catalog routes only", async ({ request }) => {
  const robots = await request.get("/robots.txt");
  expect(robots.ok()).toBe(true);
  const robotsText = await robots.text();
  expect(robotsText).toContain("Disallow: /admin");
  expect(robotsText).toContain("Sitemap:");

  const sitemap = await request.get("/sitemap.xml");
  expect(sitemap.ok()).toBe(true);
  const sitemapText = await sitemap.text();
  expect(sitemapText).toContain("/products/amd-radeon-rx-9070-xt-16gb");
  expect(sitemapText).toContain("/products?category=graphics-cards");
});
