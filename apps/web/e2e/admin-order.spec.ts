import { expect, test } from "@playwright/test";
import { availablePart, placePickupOrder } from "./support/catalog";
import { ar } from "./support/copy";

const actions = ar.admin.orders.actions;

test("the owner takes a pickup order from new to completed", async ({ page, request }) => {
  const order = await placePickupOrder(request, await availablePart(request, 5));

  await page.goto(`/ar/admin/orders?q=${order.reference}`);
  await page.getByRole("button", { name: order.reference }).click();

  for (const step of [actions.next.Confirmed, actions.next.Preparing, actions.next.Delivered]) {
    await page.getByRole("button", { name: step }).click();
    await expect(page.getByRole("button", { name: step })).toBeHidden();
  }

  await page.getByRole("button", { name: ar.admin.orders.payment.record.submit }).click();
  await expect(page.getByRole("button", { name: actions.complete.button })).toBeEnabled();

  await page.getByRole("button", { name: actions.complete.button }).click();
  await expect(page.getByRole("button", { name: actions.complete.button })).toBeHidden();
});
