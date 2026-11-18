import { expect, test } from "@playwright/test";
import { ar } from "./support/copy";
import { sellAtCounter } from "./support/pos";
import { receiveNewPart } from "./support/purchase";

// The demo parts carry no warranty label, so the flow makes its own: a part bought with a warranty, sold and handed over (which starts it).
test("a sold part with a warranty is found by its RX code", async ({ page }) => {
  const { code } = await receiveNewPart(page, { warranty: "سنة" });
  await sellAtCounter(page, code);
  await page.getByRole("button", { name: ar.admin.pos.sold.deliver }).click();
  await expect(page.getByText(ar.admin.pos.sold.delivered)).toBeVisible();

  await page.goto("/ar/warranty");
  await page.getByLabel(ar.warranty.label).fill(code);
  await page.getByRole("button", { name: ar.warranty.submit }).click();

  await expect(page).toHaveURL(/q=/);
  await expect(page.getByText(code, { exact: true })).toBeVisible();
  await expect(page.getByText("سنة", { exact: false }).first()).toBeVisible();
});
