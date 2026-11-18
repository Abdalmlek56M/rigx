import { expect, type Page } from "@playwright/test";
import { ar } from "./copy";
import { uniqueName, uniquePhone } from "./names";

const pos = ar.admin.pos;

/** Sells the stock item with this code over the counter, paid in cash in full, and waits for the confirmed-sale panel. */
export async function sellAtCounter(page: Page, code: string): Promise<void> {
  await page.goto("/ar/admin/pos");
  await page.getByRole("searchbox", { name: pos.picker.search }).fill(code);
  await page.getByRole("listitem").filter({ hasText: code }).getByRole("button", { name: pos.picker.add }).click();
  await expect(page.getByRole("cell", { name: code })).toBeVisible();

  await page.getByLabel(pos.customer.name, { exact: true }).fill(uniqueName("عميل كاشير"));
  await page.getByLabel(pos.customer.phone, { exact: true }).fill(uniquePhone());

  await page.getByRole("button", { name: pos.payments.fill }).click();
  await page.getByRole("button", { name: pos.summary.confirm }).click();
  await expect(page.getByRole("heading", { name: pos.sold.confirmed })).toBeVisible();
}
