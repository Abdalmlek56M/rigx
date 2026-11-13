import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = Boolean(process.env.API_ORIGIN) && Boolean(ownerUsername) && Boolean(ownerPassword);

test.skip(!enabled, "Builds E2E requires the isolated Preview API and owner credentials.");

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1800);
  await page.getByLabel("اسم المستخدم").fill(ownerUsername!);
  await page.locator('input[autocomplete="current-password"]').fill(ownerPassword!);
  const submit = page.getByRole("button", { name: "دخول", exact: true });
  await submit.click();
  try { await page.waitForURL(/\/account\/security$/, { timeout: 8000 }); }
  catch { await submit.click(); await page.waitForURL(/\/account\/security$/, { timeout: 15000 }); }
}

async function token(request: APIRequestContext) {
  const response = await request.get("/api/v1/identity/antiforgery");
  expect(response.ok()).toBeTruthy();
  return ((await response.json()) as { requestToken: string }).requestToken;
}

async function post(request: APIRequestContext, url: string, body: unknown) {
  const response = await request.post(url, { headers: { "X-CSRF-TOKEN": await token(request) }, data: body });
  const text = await response.text();
  expect(response.ok(), `${url} -> ${response.status()} ${text}`).toBeTruthy();
  return text ? JSON.parse(text) : null;
}

test("a published build can be added to the cart and reaches checkout", async ({ page }) => {
  test.setTimeout(180_000);
  await login(page);
  const stamp = Date.now().toString().slice(-6);
  const buildName = `QA Public Build ${stamp}`;

  const products = await (await page.request.get("/api/v1/admin/catalog/products")).json();
  const product = products.find((item: { status: string }) => item.status === "Published");
  expect(product).toBeTruthy();
  const draft = await post(page.request, "/api/v1/admin/purchasing/drafts", {
    sourceType: "Marketplace", marketplaceName: `Public Build Market ${stamp}`,
    items: [{ productId: product.id, quantity: 1, unitPrice: 1000 }],
  });
  const units = await post(page.request, `/api/v1/admin/retail/receipts/from-draft/${draft.id}`, { notes: null });
  let assembly = await post(page.request, "/api/v1/admin/retail/assemblies", { name: buildName, unitIds: [units[0].id] });
  assembly = await page.request.put(`/api/v1/admin/retail/assemblies/${assembly.id}`, {
    headers: { "X-CSRF-TOKEN": await token(page.request) },
    data: { name: buildName, publicTitleAr: null, publicTitleEn: buildName, salePrice: 2500, rowVersion: assembly.rowVersion },
  }).then((response) => response.json());
  await post(page.request, `/api/v1/admin/retail/assemblies/${assembly.id}/publish`, { rowVersion: assembly.rowVersion });

  await page.goto("/builds", { waitUntil: "domcontentloaded" });
  const card = page.locator("article").filter({ hasText: buildName });
  await expect(card).toBeVisible({ timeout: 20_000 });
  await card.getByRole("button", { name: "أضف التجميعة للسلة" }).click();
  await expect(page.getByRole("dialog", { name: /سلة التسوق/ })).toContainText(buildName);

  await page.goto("/cart");
  await expect(page.locator("li.rigx-cart-line")).toContainText(buildName);
  await expect(page.locator("aside.rigx-cart-summary")).toContainText("2,500");
  await page.getByRole("link", { name: "متابعة إلى إتمام الطلب" }).click();
  await expect(page.getByRole("heading", { name: "إتمام الطلب" })).toBeVisible();
  await expect(page.locator(".rigx-checkout-mini-lines")).toContainText(buildName);
});
