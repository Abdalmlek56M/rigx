import { expect, test } from "@playwright/test";

test.skip(!process.env.API_ORIGIN, "Comparison E2E requires the isolated Preview API.");

test("product comparison stays removed from cards, tray, and routes", async ({ page }) => {
  await page.goto("/products?category=graphics-cards");
  await expect(page.locator("article.product-card").first()).toBeVisible();

  // Compare was intentionally removed for this milestone; it must not
  // reappear as a card action, tray, or persisted preference.
  await expect(page.getByRole("button", { name: "قارن" })).toHaveCount(0);
  await expect(page.getByRole("complementary", { name: "شريط المقارنة" })).toHaveCount(0);

  const response = await page.goto("/compare");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { name: "مقارنة المنتجات" })).toHaveCount(0);
});

test("stale comparison preferences cannot restore the removed feature", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("rigx-product-comparison-v1", JSON.stringify([
      {
        id: "00000000-0000-0000-0000-000000000000",
        slug: "amd-radeon-rx-9070-xt-16gb",
        name: "Stale GPU",
        categorySlug: "graphics-cards",
      },
    ]));
  });
  await page.goto("/products");
  await expect(page.locator("article.product-card").first()).toBeVisible();
  await expect(page.getByRole("button", { name: "قارن" })).toHaveCount(0);
  await expect(page.getByRole("complementary", { name: "شريط المقارنة" })).toHaveCount(0);
});

test("product sharing copies the canonical public link without exposing an unconfigured WhatsApp action", async ({ page, context }) => {
  const baseUrl = String(test.info().project.use.baseURL);
  const origin = new URL(baseUrl).origin;
  await context.grantPermissions(["clipboard-read", "clipboard-write"], { origin });
  await page.goto("/products/amd-radeon-rx-9070-xt-16gb");
  await page.getByRole("button", { name: "مشاركة المنتج" }).first().click();
  await expect(page.getByRole("status").filter({ hasText: "تم نسخ رابط المنتج." })).toBeAttached();
  const copied = await page.evaluate(() => navigator.clipboard.readText());
  expect(copied).toBe(`${origin}/products/amd-radeon-rx-9070-xt-16gb`);
  await expect(page.getByRole("link", { name: "اطلب على واتساب" })).toHaveCount(0);
});
