import { expect, type Page } from "@playwright/test";
import { ar } from "./copy";
import { uniqueName, uniquePhone } from "./names";

const purchases = ar.admin.purchases;

/** Buys one new part from a new seller (cash) and receives it into stock; returns the model and its new RX code. */
export async function receiveNewPart(page: Page, options: { warranty?: string } = {}): Promise<{ model: string; code: string }> {
  const model = uniqueName("E2E Test GPU");

  await page.goto("/ar/admin/purchases/new");
  await page.getByRole("button", { name: purchases.seller.new }).click();
  await page.getByLabel(purchases.seller.name, { exact: true }).fill(uniqueName("بايع اختبار"));
  await page.getByLabel(purchases.seller.phone, { exact: true }).fill(uniquePhone());
  await page.getByRole("button", { name: purchases.seller.save }).click();
  await expect(page.getByRole("button", { name: purchases.seller.change })).toBeVisible();

  await page.getByRole("combobox", { name: ar.admin.inventory.fields.category, exact: true }).selectOption({ index: 1 });
  await page.getByLabel(purchases.lines.model, { exact: true }).fill(model);
  await page.getByRole("combobox", { name: purchases.lines.condition, exact: true }).selectOption("New");
  await page.getByLabel(purchases.lines.unitCost).fill("1000");
  await page.getByLabel(purchases.lines.price).fill("1500");
  if (options.warranty) await page.getByRole("combobox", { name: ar.admin.inventory.fields.warranty, exact: true }).selectOption(options.warranty);
  await page.getByLabel(purchases.payment.paid, { exact: true }).fill("1000");

  await page.getByRole("button", { name: purchases.actions.saveDraft }).click();
  await expect(page).toHaveURL(/\/admin\/purchases\/[0-9a-f-]{36}$/);

  await page.getByRole("button", { name: purchases.actions.receive }).click();
  await page.getByRole("dialog").getByRole("button", { name: purchases.actions.receive }).click();

  const link = page.getByRole("status").getByRole("link", { name: new RegExp(model) });
  const code = (await link.locator("span").first().innerText()).trim();
  expect(code).toMatch(/^RX-\d+$/);
  return { model, code };
}
