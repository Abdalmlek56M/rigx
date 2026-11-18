import { expect, test } from "@playwright/test";
import { newApi } from "./support/api";
import { ar } from "./support/copy";
import { SIGNED_OUT } from "./support/env";
import { uniqueEmail, uniqueName, uniquePhone } from "./support/names";
import { signIn } from "./support/sign-in";

test.use({ storageState: SIGNED_OUT });

test("a new customer orders a part for pickup and follows it from the tracking link", async ({ page }) => {
  const api = await newApi();
  // Guest checkout is off by default: a new email address gets a customer account through the sign-in code.
  await signIn(page, api, uniqueEmail(), "/products");
  await expect(page).toHaveURL(/\/products$/);

  const card = page.getByRole("article").filter({ has: page.getByRole("button", { name: ar.cart.add }) }).first();
  const link = card.getByRole("link").nth(1);
  const model = (await link.innerText()).trim();
  await link.click();
  await expect(page.getByRole("heading", { level: 1, name: model })).toBeVisible();

  await page.getByRole("button", { name: ar.cart.add, exact: true }).click();
  await expect(page.getByRole("status").filter({ hasText: ar.cart.added })).toBeVisible();

  await page.goto("/ar/cart");
  await expect(page.getByText(model).first()).toBeVisible();
  await page.getByRole("link", { name: ar.cartPage.checkout }).click();

  await page.getByLabel(ar.checkout.customer.name, { exact: true }).fill(uniqueName("عميل اختبار"));
  await page.getByLabel(ar.checkout.customer.phone).fill(uniquePhone());
  await page.getByRole("radio", { name: /^استلام/ }).check();
  await page.getByRole("radio", { name: ar.checkout.payment.methods.CashOnDelivery }).check();
  await page.getByRole("checkbox", { name: /^موافق على/ }).check();
  await page.getByRole("button", { name: ar.checkout.summary.confirm }).click();

  await expect(page).toHaveURL(/\/checkout\/success/);
  await expect(page.getByRole("heading", { level: 1, name: ar.orderSuccess.title })).toBeVisible();
  await expect(page.getByText(model).first()).toBeVisible();
  await page.getByRole("link", { name: ar.orderSuccess.track.track }).click();

  await expect(page).toHaveURL(/\/t\//);
  await expect(page.getByRole("heading", { level: 1, name: ar.tracking.headings.New })).toBeVisible();
  await expect(page.getByText(model).first()).toBeVisible();
  await api.dispose();
});
