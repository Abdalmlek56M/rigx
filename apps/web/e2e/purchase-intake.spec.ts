import { expect, test } from "@playwright/test";
import { receiveNewPart } from "./support/purchase";

test("buying a part from a new seller adds it to the inventory with an RX code", async ({ page }) => {
  const { model, code } = await receiveNewPart(page);

  await page.goto("/ar/admin/inventory");
  await page.getByRole("searchbox").first().fill(code);
  await expect(page.getByText(model).first()).toBeVisible();
});
