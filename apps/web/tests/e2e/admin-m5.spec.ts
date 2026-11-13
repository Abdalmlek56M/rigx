import { expect, test } from "@playwright/test";
import path from "node:path";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = process.env.E2E_M5_QA === "1" && Boolean(ownerUsername) && Boolean(ownerPassword);
const baseURL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${process.env.E2E_WEB_PORT ?? "3006"}`;
const reviewDirectory = path.resolve(process.cwd(), "..", "..", "docs", "review", "m3-hardening-m5-2026-11-08");

const localeCookie = { name: "hardware-store-locale", value: "en", url: baseURL };
const darkCookie = { name: "hardware-store-theme", value: "dark", url: baseURL };

test.skip(
  !enabled,
  "M5 admin QA requires E2E_M5_QA=1 and preview owner credentials.",
);

test("owner manages suppliers, catalog operations, warehouses, and store settings", async ({ browser }, testInfo) => {
  test.setTimeout(240_000);
  const suffix = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  const supplierName = `QA Supplier ${suffix}`;
  const warehouseCode = `QA-${suffix}`;
  const warehouseName = `QA Warehouse ${suffix}`;

  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  await context.addCookies([localeCookie, darkCookie]);
  await context.addInitScript(() => {
    if (!window.localStorage.getItem("hardware-store-theme")) {
      window.localStorage.setItem("hardware-store-theme", "dark");
    }
  });
  const page = await context.newPage();
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];
  const unauthorizedResponses: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error") consoleErrors.push(message.text());
  });
  page.on("pageerror", (error) => consoleErrors.push(String(error)));
  page.on("response", (response) => {
    if (response.status() >= 500) serverErrors.push(`${response.status()} ${response.url()}`);
    if (response.status() === 401) unauthorizedResponses.push(response.url());
  });

  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Username").fill(ownerUsername!);
  await page.locator('input[autocomplete="current-password"]').fill(ownerPassword!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await page.waitForURL(/\/account\/security$/, { timeout: 30_000 });
  await expect.poll(() => page.evaluate(() => document.documentElement.dataset.theme)).toBe("dark");
  await expect.poll(() => page.evaluate(() => document.documentElement.dir)).toBe("ltr");

  await page.evaluate(() => window.localStorage.removeItem("hardware-store-theme"));
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect.poll(() => page.evaluate(() => document.documentElement.dataset.theme)).toBe("dark");

  await page.goto("/admin/purchasing/suppliers", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Supplier directory" })).toBeVisible();

  await page.getByRole("button", { name: "New supplier" }).click();
  await page.getByLabel("Company name", { exact: true }).fill(supplierName);
  await page.getByLabel("Company phone", { exact: true }).fill("+20255550000");
  await page.getByLabel("Company email", { exact: true }).fill(`qa-${suffix}@example.test`);
  await page.getByLabel("Tax ID", { exact: true }).fill(`TAX-${suffix}`);
  await page.getByLabel("Contact person", { exact: true }).fill("QA Contact");
  await page.getByLabel("Contact phone", { exact: true }).fill("+201000000000");
  await page.getByLabel("Payment terms (days)", { exact: true }).fill("30");
  await page.getByRole("button", { name: "Create supplier" }).click();
  await expect(page.getByText("Supplier created.")).toBeVisible();

  await page.getByPlaceholder("Search by company, tax ID, or commercial registration").fill(supplierName);
  const supplierRow = page.locator("table tbody tr").filter({ hasText: supplierName }).first();
  await expect(supplierRow).toBeVisible();
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-suppliers-financials-entry-dark.png"),
    fullPage: true,
  });

  await supplierRow.getByRole("button", { name: "Financial data" }).click();
  await expect(page.getByText("Supplier financial data", { exact: false }).first()).toBeVisible();
  await page.getByLabel("Bank", { exact: true }).fill("QA Bank");
  await page.getByLabel("Account number", { exact: true }).fill("1234567890");
  await page.getByLabel("IBAN", { exact: true }).fill("EG380001000000000000000000");
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-supplier-financial-drawer-dark.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Financial data saved.")).toBeVisible();

  const listResponse = await page.request.get(
    `${baseURL}/api/v1/admin/purchasing/suppliers?search=${encodeURIComponent(supplierName)}`,
  );
  expect(listResponse.ok()).toBeTruthy();
  const listPayload = await listResponse.text();
  expect(listPayload).not.toContain("bankName");
  expect(listPayload).not.toContain("bankAccountNumber");
  expect(listPayload).not.toContain("bankIban");

  await supplierRow.getByRole("link", { name: "Supplier products" }).click();
  await expect(page.getByRole("heading", { level: 1, name: /Supplier products/ })).toBeVisible();
  await page.getByRole("button", { name: "Link product" }).click();
  const firstVariant = page.locator("div.max-h-64 button").first();
  await expect(firstVariant).toBeVisible({ timeout: 20_000 });
  await firstVariant.click();
  await page.getByLabel("Supplier code", { exact: true }).fill(`SUP-${suffix}`);
  await page.getByLabel("Notes", { exact: true }).fill("QA supplier product link");
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-supplier-product-drawer-dark.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Product link saved.")).toBeVisible();
  await expect(page.getByText(`SUP-${suffix}`)).toBeVisible();
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-supplier-products-dark.png"),
    fullPage: true,
  });

  await page.goto("/admin/inventory/warehouses", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Warehouse directory" })).toBeVisible();
  await page.getByRole("button", { name: "New warehouse" }).click();
  await page.getByLabel("Warehouse code", { exact: true }).fill(warehouseCode);
  await page.getByLabel("Warehouse name", { exact: true }).fill(warehouseName);
  await page.getByLabel("Address", { exact: true }).fill("Cairo QA");
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-warehouse-drawer-dark.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Create warehouse" }).click();
  await expect(page.getByText("Warehouse created.")).toBeVisible();
  await page.getByPlaceholder("Search by code, name, or address").fill(warehouseCode);
  const warehouseRow = page.locator("table tbody tr").filter({ hasText: warehouseCode }).first();
  await expect(warehouseRow).toBeVisible();
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-warehouses-dark.png"),
    fullPage: true,
  });

  await warehouseRow.getByRole("button", { name: "Edit warehouse" }).click();
  const warehouseNameInput = page.getByLabel("Warehouse name", { exact: true });
  await expect(warehouseNameInput).toHaveValue(warehouseName);
  await warehouseNameInput.fill(`${warehouseName} Updated`);
  await expect(warehouseNameInput).toHaveValue(`${warehouseName} Updated`);
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Warehouse updated.")).toBeVisible();
  await expect(
    page.locator("table tbody tr").filter({ hasText: warehouseCode }).getByText(`${warehouseName} Updated`),
  ).toBeVisible();

  await page.goto("/admin/storefront/settings", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Store settings" })).toBeVisible();
  await page.getByLabel("WhatsApp number", { exact: true }).fill("201000000000");
  await page.getByLabel("Opening WhatsApp message", { exact: true }).fill("QA WhatsApp message");
  await page.getByLabel("Facebook", { exact: true }).fill("https://facebook.com/rigx");
  await page.getByLabel("Instagram", { exact: true }).fill("https://instagram.com/rigx");
  await page.getByLabel("Pickup address", { exact: true }).fill("Cairo QA pickup");
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-store-settings-dark.png"),
    fullPage: true,
  });
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Store settings saved.")).toBeVisible();

  await page.goto("/", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("link", { name: "Chat on WhatsApp" })).toBeVisible();
  await expect(page.locator('footer a[aria-label="Facebook"]')).toBeVisible();
  await expect(page.locator('footer a[aria-label="Instagram"]')).toBeVisible();
  await page.screenshot({
    path: path.join(reviewDirectory, "storefront-whatsapp-footer-dark.png"),
    fullPage: true,
  });

  await page.goto("/admin/inventory/warehouses", { waitUntil: "domcontentloaded" });
  await context.addCookies([{ name: "hardware-store-theme", value: "light", url: baseURL }]);
  await page.evaluate(() => window.localStorage.setItem("hardware-store-theme", "light"));
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Warehouse directory" })).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.dataset.theme)).toBe("light");
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-warehouses-light.png"),
    fullPage: true,
  });

  const mobileContext = await browser.newContext({
    viewport: { width: 390, height: 844 },
    storageState: await context.storageState(),
    isMobile: true,
    hasTouch: true,
  });
  await mobileContext.addInitScript(() => {
    window.localStorage.setItem("hardware-store-theme", "dark");
  });
  const mobilePage = await mobileContext.newPage();
  await mobilePage.goto("/admin/inventory/warehouses", { waitUntil: "domcontentloaded" });
  await expect(mobilePage.getByRole("heading", { level: 1, name: "Warehouse directory" })).toBeVisible();
  await expect.poll(() => mobilePage.evaluate(() => document.documentElement.dataset.theme)).toBe("dark");
  await mobilePage.getByPlaceholder("Search by code, name, or address").fill(warehouseCode);
  await expect(mobilePage.getByText(`${warehouseName} Updated`).first()).toBeVisible();
  await mobilePage.screenshot({
    path: path.join(reviewDirectory, "admin-warehouses-mobile-dark.png"),
    fullPage: true,
  });
  await mobilePage.goto("/admin/purchasing/suppliers", { waitUntil: "domcontentloaded" });
  await mobilePage.getByPlaceholder("Search by company, tax ID, or commercial registration").fill(supplierName);
  await expect(mobilePage.getByText(supplierName).first()).toBeVisible();
  await mobilePage.screenshot({
    path: path.join(reviewDirectory, "admin-suppliers-mobile-financials-dark.png"),
    fullPage: true,
  });

  await mobileContext.close();

  await context.addCookies([
    { name: "hardware-store-locale", value: "ar", url: baseURL },
    { name: "hardware-store-theme", value: "dark", url: baseURL },
  ]);
  await page.evaluate(() => {
    window.localStorage.setItem("hardware-store-locale", "ar");
    window.localStorage.setItem("hardware-store-theme", "dark");
  });
  await page.reload({ waitUntil: "domcontentloaded" });
  await expect.poll(() => page.evaluate(() => document.documentElement.dir)).toBe("rtl");
  await expect.poll(() => page.evaluate(() => document.documentElement.dataset.theme)).toBe("dark");
  await expect(page.getByRole("heading", { level: 1, name: "دليل المستودعات" })).toBeVisible();
  await page.screenshot({
    path: path.join(reviewDirectory, "admin-warehouses-ar-dark.png"),
    fullPage: true,
  });

  await context.close();

  const unexpectedConsoleErrors = consoleErrors.filter(
    (message) => !message.includes("401 (Unauthorized)"),
  );
  expect.soft(serverErrors, `Server errors: ${serverErrors.join(" | ")}`).toEqual([]);
  expect.soft(
    unauthorizedResponses.filter((url) => !url.endsWith("/api/v1/identity/me")),
    `Unexpected unauthorized responses: ${unauthorizedResponses.join(" | ")}`,
  ).toEqual([]);
  expect.soft(unexpectedConsoleErrors, `Console errors: ${unexpectedConsoleErrors.join(" | ")}`).toEqual([]);
  await testInfo.attach("qa-summary", {
    body: JSON.stringify({ supplierName, warehouseCode, consoleErrors, serverErrors }, null, 2),
    contentType: "application/json",
  });
});


