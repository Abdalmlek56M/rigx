import { expect, test, type Page } from "@playwright/test";

test.skip(!process.env.API_ORIGIN, "Cart E2E requires the isolated Preview API.");

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;

const gpuName = "AMD Radeon RX 9070 XT 16GB";
const cpuName = "AMD Ryzen 7 9800X3D";

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

function card(page: import("@playwright/test").Page, name: string) {
  return page.locator("article.product-card").filter({
    has: page.getByRole("heading", { name }),
  });
}

test("cart adds, merges, persists, and updates totals before removal", async ({ page }) => {
  await page.goto("/products");
  await expect(page.getByRole("heading", { name: gpuName })).toBeVisible();

  const gpuCard = card(page, gpuName);
  await gpuCard.getByRole("button", { name: "أضف إلى السلة" }).click();
  const toast = page.getByRole("status").filter({ hasText: "تمت إضافة المنتج إلى السلة." });
  await expect(toast).toBeVisible();
  await expect(toast).toContainText(gpuName);
  await expect(page.getByRole("dialog", { name: /سلة التسوق/ })).toHaveCount(0);

  await toast.getByRole("button", { name: "عرض السلة" }).click();
  const drawer = page.getByRole("dialog", { name: /سلة التسوق/ });
  await expect(drawer).toBeVisible();
  await expect(drawer).toContainText(gpuName);

  await drawer.getByRole("button", { name: "إغلاق سلة التسوق" }).click();
  await expect(drawer).toHaveCount(0);

  await card(page, gpuName).getByRole("button", { name: "أضف إلى السلة" }).click();
  const secondToast = page.getByRole("status").filter({ hasText: "تمت إضافة المنتج إلى السلة." });
  await expect(secondToast).toBeVisible();
  await secondToast.getByRole("button", { name: "عرض السلة" }).click();
  await expect(page.getByRole("dialog", { name: /سلة التسوق/ })).toContainText("× 2");

  await page.getByRole("dialog", { name: /سلة التسوق/ }).getByRole("link", { name: "عرض السلة" }).click();
  await expect(page).toHaveURL(/\/cart$/);
  await expect(page.getByRole("heading", { name: "سلة التسوق" })).toBeVisible();

  const line = page.locator("li.rigx-cart-line").filter({ hasText: gpuName });
  await expect(line).toContainText("× 2");
  const summary = page.locator("aside.rigx-cart-summary");
  const subtotalText = await summary.locator(".rigx-cart-summary-row strong").innerText();
  const subtotal = Number(subtotalText.replace(/[^\d.]/g, ""));
  const lineTotalText = await line.locator(".rigx-cart-line-price strong").innerText();
  const lineTotal = Number(lineTotalText.replace(/[^\d.]/g, ""));
  expect(subtotal).toBeCloseTo(lineTotal, 0);

  await line.getByRole("button", { name: "زيادة الكمية" }).click();
  await expect(line).toContainText("3");
  const increased = Number((await summary.locator(".rigx-cart-summary-row strong").innerText()).replace(/[^\d.]/g, ""));
  expect(increased).toBeCloseTo((lineTotal / 2) * 3, 0);

  await page.reload();
  await expect(page.locator("li.rigx-cart-line").filter({ hasText: gpuName })).toContainText("3");

  await page.getByRole("button", { name: "إفراغ السلة" }).click();
  const confirm = page.getByRole("alertdialog", { name: "إفراغ السلة" });
  await expect(confirm).toBeVisible();
  await confirm.getByRole("button", { name: "إفراغ السلة" }).click();
  await expect(page.getByRole("heading", { name: "سلتك فارغة" })).toBeVisible();
});

test("variants stay separate and real checkout validates, creates a backend order, and clears the cart", async ({ page }) => {
  test.skip(!ownerUsername || !ownerPassword, "Real checkout requires an authenticated customer account.");
  await login(page);
  await page.goto("/products/amd-radeon-rx-9070-xt-16gb");
  await expect(page.getByRole("heading", { name: gpuName })).toBeVisible();

  await page.getByRole("button", { name: /OC Edition/ }).click();
  await expect(page.getByText("SKU: GPU-9070XT-OC")).toBeVisible();
  await page.getByRole("button", { name: "أضف إلى السلة" }).first().click();
  const variantToast = page.getByRole("status").filter({ hasText: "تمت إضافة المنتج إلى السلة." });
  await expect(variantToast).toContainText("OC Edition");
  await variantToast.getByRole("button", { name: "عرض السلة" }).click();
  await expect(page.getByRole("dialog", { name: /سلة التسوق/ })).toContainText("OC Edition");
  await page.getByRole("dialog", { name: /سلة التسوق/ }).getByRole("button", { name: "إغلاق سلة التسوق" }).click();

  await page.goto("/products");
  await card(page, cpuName).getByRole("button", { name: "أضف إلى السلة" }).click();
  await page.getByRole("status").filter({ hasText: "تمت إضافة المنتج إلى السلة." }).getByRole("button", { name: "عرض السلة" }).click();
  await page.getByRole("dialog", { name: /سلة التسوق/ }).getByRole("link", { name: "عرض السلة" }).click();
  await expect(page.locator("li.rigx-cart-line")).toHaveCount(2);

  await page.getByRole("link", { name: "متابعة إلى إتمام الطلب" }).click();
  await expect(page).toHaveURL(/\/checkout$/);
  await expect(page.getByRole("heading", { name: "إتمام الطلب" })).toBeVisible();

  await page.getByRole("button", { name: "تأكيد الطلب" }).click();
  await expect(page.getByText("هذا الحقل مطلوب.")).toHaveCount(4);

  await page.getByLabel("الاسم الكامل").fill("عميل اختبار");
  await page.getByLabel("رقم الهاتف").fill("01000000000");
  await page.getByLabel("المدينة").fill("القاهرة");
  await page.getByLabel("العنوان").fill("شارع اختبار 1");
  await page.locator("textarea").fill("طلب اختبار آلي");
  await page.getByRole("checkbox").check();

  await page.getByRole("button", { name: "تأكيد الطلب" }).click();
  await expect(page.getByRole("heading", { name: "تم إنشاء طلبك" })).toBeVisible();
  const reference = (await page.getByText(/^RIGX-\d{8}-\d{6}$/).innerText()).trim();
  await expect(page.getByText(gpuName)).toBeVisible();
  await expect(page.getByText(cpuName)).toBeVisible();

  const stored = await page.evaluate(() => localStorage.getItem("rigx-cart-v1"));
  expect(JSON.parse(stored ?? "[]")).toEqual([]);

  await page.goto("/account/orders");
  await expect(page.getByText(reference)).toBeVisible();

  await page.goto("/cart");
  await expect(page.getByRole("heading", { name: "سلتك فارغة" })).toBeVisible();
});

test("buy now uses the selected variant and opens checkout", async ({ page }) => {
  await page.goto("/products/amd-radeon-rx-9070-xt-16gb");
  await page.getByRole("button", { name: /OC Edition/ }).click();
  await expect(page.getByText("SKU: GPU-9070XT-OC")).toBeVisible();

  await page.getByRole("button", { name: "اشتري الآن" }).click();
  await expect(page).toHaveURL(/\/checkout$/);
  await expect(page.getByRole("heading", { name: "إتمام الطلب" })).toBeVisible();
  const checkoutLines = page.locator(".rigx-checkout-mini-lines");
  await expect(checkoutLines).toContainText(gpuName);
  await expect(checkoutLines).toContainText("OC Edition");

  const stored = await page.evaluate(() => localStorage.getItem("rigx-cart-v1"));
  const items = JSON.parse(stored ?? "[]") as Array<{ variantName: string | null }>;
  expect(items).toHaveLength(1);
  expect(items[0]?.variantName).toBe("OC Edition");
});

test("cart and checkout stay usable at 390px in both themes without horizontal overflow", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/products");
  await card(page, gpuName).getByRole("button", { name: "أضف إلى السلة" }).click();
  const toast = page.getByRole("status").filter({ hasText: "تمت إضافة المنتج إلى السلة." });
  await expect(toast).toBeVisible();
  await expect(page.getByRole("dialog", { name: /سلة التسوق/ })).toHaveCount(0);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
  await toast.getByRole("button", { name: "عرض السلة" }).click();
  const drawer = page.getByRole("dialog", { name: /سلة التسوق/ });
  await expect(drawer).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
  await drawer.getByRole("button", { name: "إغلاق سلة التسوق" }).click();

  await page.evaluate(() => localStorage.setItem("hardware-store-theme", "light"));
  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "light");
  await page.evaluate(() => localStorage.setItem("hardware-store-theme", "dark"));
  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.goto("/checkout");
  await expect(page.getByRole("heading", { name: "إتمام الطلب" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/cart");
  await expect(page.getByRole("heading", { name: "سلة التسوق" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
});

