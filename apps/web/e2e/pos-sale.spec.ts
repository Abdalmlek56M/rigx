import { expect, test } from "@playwright/test";
import { availablePart } from "./support/catalog";
import { ar } from "./support/copy";
import { sellAtCounter } from "./support/pos";

test("a counter sale is paid in cash and confirmed", async ({ page, request }) => {
  await sellAtCounter(page, (await availablePart(request, 9)).code);
  await expect(page.getByRole("link", { name: ar.admin.pos.sold.openOrder })).toBeVisible();
});
