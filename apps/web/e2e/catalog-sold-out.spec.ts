import { expect, test } from "@playwright/test";
import { getJson } from "./support/api";
import { ar } from "./support/copy";
import { sellAtCounter } from "./support/pos";

interface StorePage {
  items: { code: string; modelName: string; condition: string; available: number }[];
}

test("a sold part stays in the catalog, after the available ones, and the old sold page is gone", async ({ page, request }) => {
  // A used part is a single piece: selling it at the counter leaves it sold out.
  const { items } = await getJson<StorePage>(request, "/api/v1/store/items?pageSize=48&condition=Used&inStock=true");
  const part = items.at(-1);
  if (!part) throw new Error("The demo store has no used part in stock; reset the demo data.");
  await sellAtCounter(page, part.code);

  await page.goto(`/ar/products?search=${encodeURIComponent(part.code)}`);
  const card = page.getByRole("article").filter({ hasText: part.modelName });
  await expect(card.getByText(ar.item.soldOutBadge, { exact: true })).toBeVisible();

  await page.goto("/ar/products");
  const cards = page.getByRole("article");
  await expect(cards.first().getByRole("button", { name: ar.cart.add })).toBeVisible();
  await page.getByRole("link", { name: ar.catalog.filters.inStock }).first().click();
  await expect(page).toHaveURL(/stock=in/);
  await expect(cards.filter({ hasText: ar.item.soldOutBadge })).toHaveCount(0);

  const sold = await page.goto("/ar/sold");
  expect(sold?.status()).toBe(404);
});
