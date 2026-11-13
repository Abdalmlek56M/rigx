import { expect, test, type Page } from "@playwright/test";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = process.env.E2E_MAIN_QA === "1" && Boolean(ownerUsername) && Boolean(ownerPassword);

test.skip(!enabled, "Main engineering E2E requires E2E_MAIN_QA=1 and owner credentials.");

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

test("header exposes offers and the offers page uses published data", async ({ page }) => {
  await page.goto("/", { waitUntil: "domcontentloaded" });
  const nav = page.getByRole("navigation", { name: "التنقل الأساسي" });
  await expect(nav.getByRole("link", { name: "العروض" })).toBeVisible();
  await nav.getByRole("link", { name: "العروض" }).click();
  await expect(page).toHaveURL(/\/offers$/);
  await expect(page.getByRole("heading", { name: "العروض الحالية" })).toBeVisible();
});

test("account center is the account gateway with clear sections and avatar support", async ({ page }) => {
  await login(page);
  await page.goto("/account", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "مركز الحساب" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "مركز الحساب" })).toBeVisible();
  await expect(page.getByRole("link", { name: "العناوين المحفوظة" }).first()).toBeVisible();
  await expect(page.getByRole("link", { name: "قائمة الرغبات" }).first()).toBeVisible();
  await expect(page.getByRole("link", { name: "تقييماتي" }).first()).toBeVisible();

  await page.getByRole("link", { name: "البيانات الشخصية" }).first().click();
  await page.waitForURL(/\/account\/profile$/);
  await expect(page.getByRole("button", { name: "تغيير الصورة" })).toBeVisible({ timeout: 15000 });
});

test("catalog navigation has one active item and management stays a list", async ({ page }) => {
  await login(page);
  await page.goto("/admin/catalog", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "إدارة الكتالوج" })).toBeVisible();
  const active = page.locator('aside [aria-current="page"]');
  await expect(active).toHaveCount(1);

  await page.goto("/admin/catalog/products/new", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "منتج جديد" })).toBeVisible();
  await expect(page.locator('aside [aria-current="page"]')).toHaveCount(1);
});

test("review flow requires moderation then publishes with aggregate", async ({ page }) => {
  await login(page);
  await page.goto("/products/amd-ryzen-7-9800x3d", { waitUntil: "domcontentloaded" });
  await page.getByRole("heading", { name: "تقييمات العملاء" }).scrollIntoViewIfNeeded();
  const body = `E2E review ${Date.now()} — solid performance and clear packaging.`;
  await page.getByRole("button", { name: "التقييم 5 / 5" }).click();
  await page.getByLabel("نص التقييم").fill(body);
  await page.getByRole("button", { name: "إرسال التقييم" }).click();
  await expect(page.getByText(/قيد المراجعة/).first()).toBeVisible();

  await page.goto("/account/reviews", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "تقييماتي" })).toBeVisible();
  await expect(page.getByText(body)).toBeVisible();

  await page.goto("/admin/reviews", { waitUntil: "domcontentloaded" });
  const row = page.locator("article").filter({ hasText: body });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "تفاصيل الحدث" }).click();
  await page.getByRole("dialog").getByRole("button", { name: "نشر" }).click();
  await expect(page.getByText("تم النشر.")).toBeVisible();

  await page.goto("/products/amd-ryzen-7-9800x3d", { waitUntil: "domcontentloaded" });
  await expect(page.locator("article").filter({ hasText: body })).toBeVisible();
});

test("purchase draft can be created from a marketplace without a supplier and cancelled", async ({ page }) => {
  await login(page);
  await page.goto("/admin/purchasing/drafts", { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "مسودة جديدة" }).click();
  const drawer = page.getByRole("dialog");
  await drawer.getByLabel("اسم السوق").fill(`E2E Market ${Date.now()}`);
  await drawer.getByLabel("اسم البائع").fill("E2E Seller");
  await drawer.getByLabel("رابط الإعلان").fill("https://example.test/listing/qa");
  const productSelect = drawer.locator("select").nth(1);
  await productSelect.locator("option").nth(1).waitFor({ state: "attached" });
  const productValue = await productSelect.locator("option").nth(1).getAttribute("value");
  await productSelect.selectOption(productValue!);
  const quantity = drawer.locator('input[type="number"]').nth(0);
  const price = drawer.locator('input[type="number"]').nth(1);
  await quantity.fill("2");
  await price.fill("1500");
  await drawer.getByRole("button", { name: "حفظ المسودة" }).click();
  await expect(page.getByText("تم إنشاء المسودة.")).toBeVisible();
  const draftRow = page.locator("article").filter({ hasText: "E2E Seller" }).first();
  await expect(draftRow).toBeVisible();
  await expect(draftRow.getByText(/3,000/)).toBeVisible();
  await draftRow.getByRole("button", { name: "إلغاء المسودة" }).click();
  await expect(page.getByText("ملغاة").first()).toBeVisible();
});

test("admin dashboard renders operational analytics from real data", async ({ page }) => {
  await login(page);
  await page.goto("/admin", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "تحليلات تشغيلية" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "توزيع التقييمات المنشورة" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "آخر أحداث الأمان" })).toBeVisible();
});

test("PC Builder admin template publishes to the public builder", async ({ page }) => {
  await login(page);
  await page.goto("/admin/pc-builder", { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "قالب جديد" }).click();
  const titleAr = `E2E Build ${Date.now()}`;
  await page.getByLabel("العنوان العربي").fill(titleAr);
  await page.getByLabel("العنوان الإنجليزي").fill("E2E Build");
  await page.getByPlaceholder("اسم الفتحة بالعربية").fill("المعالج");
  await page.getByPlaceholder("اسم الفتحة بالإنجليزية").fill("CPU");
  const productSelect = page.locator("select").filter({ hasText: "اختر منتجًا منشورًا" }).first();
  await productSelect.locator("option").nth(1).waitFor({ state: "attached" });
  await productSelect.selectOption(await productSelect.locator("option").nth(1).getAttribute("value"));
  await page.getByRole("button", { name: "حفظ التعديلات" }).click();
  await expect(page.getByText("تم حفظ القالب.")).toBeVisible();
  const row = page.locator("article").filter({ hasText: titleAr });
  await expect(row).toBeVisible();
  const csrf = await page.request.get("/api/v1/identity/antiforgery");
  const token = (await csrf.json() as { requestToken: string }).requestToken;
  const buildsResponse = await page.request.get("/api/v1/admin/pc-builder/templates");
  const builds = await buildsResponse.json() as Array<{ id: string; titleAr: string; rowVersion: number }>;
  const published = builds.find((build) => build.titleAr === titleAr)!;
  const publish = await page.request.post(`/api/v1/admin/pc-builder/templates/${published.id}/publish`, {
    headers: { "X-CSRF-TOKEN": token },
    data: { rowVersion: published.rowVersion },
  });
  expect(publish.status()).toBe(200);

  await page.goto("/pc-builder", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "مجمّع الحاسب" })).toBeVisible();
  await expect(page.getByRole("heading", { name: titleAr })).toBeVisible();
  await page.getByRole("link", { name: "ابدأ التجميع" }).first().click();
  await expect(page.getByText("ملخص التجميع")).toBeVisible();
  await expect(page.getByText("التوافق", { exact: false }).first()).toBeVisible();
});

test("purchase draft accepts a manual item with no catalog product", async ({ page }) => {
  await login(page);
  await page.goto("/admin/purchasing/drafts", { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "مسودة جديدة" }).click();
  const drawer = page.getByRole("dialog");
  await drawer.getByLabel("اسم السوق").fill(`E2E Manual Market ${Date.now()}`);
  const manualName = `Manual Monitor ${Date.now()}`;
  await drawer.getByPlaceholder("اسم الصنف اليدوي (غير موجود في الكتالوج)").fill(manualName);
  await drawer.getByPlaceholder("كود خارجي / SKU").fill("EXT-001");
  await drawer.locator('input[type="number"]').nth(0).fill("1");
  await drawer.locator('input[type="number"]').nth(1).fill("900");
  await drawer.getByRole("button", { name: "حفظ المسودة" }).click();
  await expect(page.getByText("تم إنشاء المسودة.")).toBeVisible();
  const row = page.locator("article").filter({ hasText: "E2E Manual Market" }).first();
  await expect(row).toBeVisible();
  await expect(row.getByText(/1 items/)).toBeVisible();
});
