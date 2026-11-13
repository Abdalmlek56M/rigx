import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = Boolean(process.env.API_ORIGIN) && Boolean(ownerUsername) && Boolean(ownerPassword);

test.skip(!enabled, "Retail core E2E requires the isolated Preview API and owner credentials.");

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(1800);
  await page.getByLabel("اسم المستخدم").fill(ownerUsername!);
  await page.locator('input[autocomplete="current-password"]').fill(ownerPassword!);
  const submit = page.getByRole("button", { name: "دخول", exact: true });
  await submit.click();
  try {
    await page.waitForURL(/\/account\/security$/, { timeout: 8000 });
  } catch {
    await submit.click();
    await page.waitForURL(/\/account\/security$/, { timeout: 15000 });
  }
}

async function csrf(request: APIRequestContext): Promise<string> {
  const response = await request.get("/api/v1/identity/antiforgery");
  expect(response.ok()).toBeTruthy();
  return ((await response.json()) as { requestToken: string }).requestToken;
}

async function postJson(request: APIRequestContext, url: string, body: unknown) {
  const token = await csrf(request);
  const response = await request.post(url, { headers: { "X-CSRF-TOKEN": token }, data: body });
  const text = await response.text();
  expect(response.ok(), `${url} -> ${response.status()} ${text}`).toBeTruthy();
  return text ? JSON.parse(text) : null;
}

async function getJson(request: APIRequestContext, url: string) {
  const response = await request.get(url);
  expect(response.ok(), `${url} -> ${response.status()}`).toBeTruthy();
  return await response.json();
}

test("receive, price, publish, sell, complete, and settle through the retail admin UI", async ({ page }) => {
  test.setTimeout(240_000);
  await login(page);

  const stamp = Date.now().toString().slice(-6);
  const marketName = `QA Market ${stamp}`;
  const buildName = `QA Build ${stamp}`;
  const partnerName = `QA Partner ${stamp}`;
  const salePrice = 2500;
  const unitCost = 1000;
  const expectedEarning = ((salePrice - unitCost) * 0.1).toFixed(0);

  const products = await getJson(page.request, "/api/v1/admin/catalog/products");
  const product = products.find((item: { status: string; basePrice: number }) => item.status === "Published" && item.basePrice > 0);
  expect(product, "A published catalog product is required for this E2E.").toBeTruthy();

  const draft = await postJson(page.request, "/api/v1/admin/purchasing/drafts", {
    sourceType: "Marketplace",
    marketplaceName: marketName,
    items: [{ productId: product.id, quantity: 1, unitPrice: unitCost }],
  });

  await page.goto("/admin/inventory/stock", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "المخزون والتجميعات" })).toBeVisible({ timeout: 30_000 });
  await page.locator("select").selectOption(draft.id);
  await page.getByRole("button", { name: "تأكيد الاستلام" }).click();
  await expect(page.getByText("تم استلام الوحدات.")).toBeVisible({ timeout: 20_000 });

  const unitRow = page.locator("label").filter({ hasText: product.name }).first();
  await expect(unitRow).toBeVisible({ timeout: 20_000 });
  await unitRow.getByRole("checkbox").check();
  await page.getByPlaceholder("اسم التجميعة").fill(buildName);
  await page.getByRole("button", { name: "إنشاء تجميعة" }).click();
  await expect(page.getByText("تم إنشاء التجميعة.")).toBeVisible({ timeout: 20_000 });

  const assemblyRow = page.locator("article").filter({ hasText: buildName });
  await expect(assemblyRow).toBeVisible({ timeout: 20_000 });
  await assemblyRow.getByRole("textbox", { name: `سعر البيع ${buildName}` }).fill(String(salePrice));
  await assemblyRow.getByRole("button", { name: "حفظ السعر" }).click();
  await expect(page.getByText("تم الحفظ.")).toBeVisible({ timeout: 20_000 });
  await assemblyRow.getByRole("button", { name: "نشر" }).click();
  await expect(assemblyRow.getByText("Published")).toBeVisible({ timeout: 20_000 });

  for (const agreement of await getJson(page.request, "/api/v1/admin/retail/partner/agreements")) {
    if (agreement.isActive && agreement.partnerName !== partnerName) {
      await postJson(page.request, "/api/v1/admin/retail/partner/agreements", {
        partnerName: agreement.partnerName,
        defaultPercent: agreement.defaultPercent,
        isActive: false,
        lossSharingEnabled: agreement.lossSharingEnabled ?? false,
        effectiveFrom: agreement.effectiveFrom,
        effectiveTo: agreement.effectiveTo,
      });
    }
  }
  await postJson(page.request, "/api/v1/admin/retail/partner/agreements", {
    partnerName, defaultPercent: 10, isActive: true, lossSharingEnabled: false,
    effectiveFrom: new Date().toISOString().slice(0, 10), effectiveTo: null,
  });

  const order = await postJson(page.request, "/api/v1/orders", {
    customerName: "QA Retail Customer",
    phoneNumber: "01000000000",
    addressText: "Cairo",
    fulfillmentMethod: "Delivery",
    paymentMethod: "CashOnDelivery",
    notes: null,
    leadSource: "Website",
    termsAccepted: true,
    items: [{ assemblyId: (await getAssemblyId(page, buildName)), quantity: 1 }],
  });

  await page.goto("/admin/orders", { waitUntil: "domcontentloaded" });
  await expect(page.getByText(order.reference, { exact: true })).toBeVisible({ timeout: 30_000 });
  const orderRow = page.locator("article").filter({ hasText: order.reference });
  await orderRow.getByRole("button", { name: "Confirmed" }).click();
  await expect(orderRow.getByText("Confirmed", { exact: false })).toBeVisible({ timeout: 20_000 });
  for (const status of ["Preparing", "Ready", "Delivered"]) {
    await orderRow.getByRole("button", { name: status }).click();
    await expect(orderRow.getByText(status, { exact: false })).toBeVisible({ timeout: 20_000 });
  }
  await orderRow.getByRole("button", { name: "تأكيد الدفع" }).click();
  await expect(orderRow.getByText("Paid", { exact: false })).toBeVisible({ timeout: 20_000 });
  await orderRow.getByRole("button", { name: "إكمال الطلب" }).click();
  await expect(orderRow.getByText("Completed", { exact: false })).toBeVisible({ timeout: 20_000 });

  await page.goto("/admin/partners", { waitUntil: "domcontentloaded" });
  await expect(page.getByText(partnerName).first()).toBeVisible({ timeout: 30_000 });
  const ledgerRow = page.locator("div").filter({ hasText: partnerName }).filter({ hasText: "مطلوب نقدًا" }).last();
  await expect(ledgerRow).toBeVisible({ timeout: 20_000 });
  await expect(ledgerRow).toContainText(expectedEarning);
  await ledgerRow.getByRole("button", { name: "تسوية النقد المستحق" }).click();
  await expect(page.getByText("تم الحفظ.")).toBeVisible({ timeout: 20_000 });

  await page.goto("/admin/orders", { waitUntil: "domcontentloaded" });
  const paidRow = page.locator("article").filter({ hasText: order.reference });
  await paidRow.getByRole("button", { name: "فتح مرتجع" }).first().click();
  await paidRow.getByLabel("قيمة الاسترداد").fill("2500");
  await paidRow.getByLabel("سبب الإرجاع").fill("QA return after payout");
  await paidRow.getByRole("button", { name: "فتح مرتجع" }).last().click();
  await expect(page.getByText("تم فتح المرتجع بانتظار الفحص.")).toBeVisible({ timeout: 20_000 });

  await page.getByRole("button", { name: "فحص", exact: true }).click();
  await page.locator("select").last().selectOption("Used");
  await page.getByPlaceholder("ملاحظات الفحص").fill("QA inspected");
  await page.getByRole("button", { name: "فحص", exact: true }).last().click();
  await expect(page.getByText("تم فحص المرتجع وتحديث المخزون.")).toBeVisible({ timeout: 20_000 });

  await page.goto("/admin/partners", { waitUntil: "domcontentloaded" });
  await expect(page.getByText("رصيد قابل للاسترداد").first()).toBeVisible({ timeout: 20_000 });
  await expect(page.getByText(/-150/).first()).toBeVisible({ timeout: 20_000 });
});

async function getAssemblyId(page: Page, buildName: string): Promise<string> {
  const assemblies = await getJson(page.request, "/api/v1/admin/retail/assemblies");
  const assembly = assemblies.find((item: { name: string }) => item.name === buildName);
  expect(assembly, "The priced assembly should be published.").toBeTruthy();
  return assembly.id;
}
