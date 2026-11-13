import { expect, test, type Page } from "@playwright/test";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = process.env.E2E_CONTENT_QA === "1" && Boolean(ownerUsername) && Boolean(ownerPassword);

test.skip(!enabled, "Content E2E requires E2E_CONTENT_QA=1 and owner credentials.");

async function login(page: Page) {
  await page.goto("/login");
  await page.getByLabel("اسم المستخدم").fill(ownerUsername!);
  await page.locator('input[autocomplete="current-password"]').fill(ownerPassword!);
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page).toHaveURL(/\/account\/security$/);
}

test("wishlist saves a product for the account and removes it again", async ({ page }) => {
  await login(page);
  await page.goto("/account/wishlist");
  const clear = page.getByRole("button", { name: "تفريغ القائمة" });
  if (await clear.isVisible().catch(() => false)) {
    await clear.click();
    await page.getByRole("alertdialog").getByRole("button", { name: "تفريغ القائمة" }).click();
    await expect(page.getByText(/قائمة الرغبات فارغة/)).toBeVisible();
  }

  await page.goto("/products/amd-ryzen-7-9800x3d");
  const add = page.getByRole("button", { name: "أضف إلى قائمة الرغبات" }).first();
  if (await add.isVisible()) await add.click();

  await page.goto("/account/wishlist");
  await expect(page.getByRole("link", { name: /AMD Ryzen 7 9800X3D/i }).first()).toBeVisible();

  await page.getByRole("button", { name: "إزالة من قائمة الرغبات" }).first().click();
  await expect(page.getByText(/قائمة الرغبات فارغة/)).toBeVisible();
});

test("customer address is created, becomes default, and is deleted", async ({ page }) => {
  await login(page);
  const label = `E2E ${Date.now()}`;
  await page.goto("/account/addresses");
  await page.getByRole("button", { name: "عنوان جديد" }).click();

  const drawer = page.getByRole("dialog");
  await drawer.getByLabel("التسمية").fill(label);
  await drawer.getByLabel("اسم المستلم").fill("E2E Recipient");
  await drawer.getByLabel("الهاتف").fill("1000000000");
  await drawer.getByLabel("المحافظة").fill("القاهرة");
  await drawer.getByLabel("المدينة").fill("القاهرة");
  await drawer.getByLabel("الشارع").fill("شارع الاختبار");
  await drawer.getByLabel("اجعله العنوان الافتراضي").check();
  await drawer.getByRole("button", { name: "حفظ التعديلات" }).click();

  const card = page.locator("article").filter({ hasText: label });
  await expect(card).toBeVisible();
  await expect(card.getByText("الافتراضي")).toBeVisible();

  await card.getByRole("button", { name: "حذف العنوان" }).click();
  const confirm = page.getByRole("alertdialog");
  await confirm.getByRole("button", { name: "حذف العنوان" }).click();
  await expect(page.locator("article").filter({ hasText: label })).toHaveCount(0);
});

test("FAQ CMS publishes to the public page and unpublishes", async ({ page }) => {
  await login(page);
  const question = `E2E question ${Date.now()}`;
  await page.goto("/admin/content/faqs");
  await page.getByRole("button", { name: "سؤال جديد" }).click();

  const drawer = page.getByRole("dialog");
  await drawer.getByLabel("السؤال العربي").fill(question);
  await drawer.getByLabel("السؤال الإنجليزي").fill("E2E question");
  await drawer.getByLabel("الإجابة العربية").fill("إجابة اختبار");
  await drawer.getByLabel("الإجابة الإنجليزية").fill("Test answer");
  await drawer.getByRole("button", { name: "حفظ التعديلات" }).click();

  const row = page.locator("article").filter({ hasText: question });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: "نشر", exact: true }).click();
  await expect(row.getByText("منشور")).toBeVisible();

  await page.goto("/faq");
  await expect(page.getByText(question)).toBeVisible();

  await page.goto("/admin/content/faqs");
  const publishedRow = page.locator("article").filter({ hasText: question });
  await publishedRow.getByRole("button", { name: "إلغاء النشر" }).click();
  await expect(publishedRow.getByText("مسودة")).toBeVisible();

  await page.goto("/faq");
  await expect(page.getByText(question)).toHaveCount(0);
});

test("content admin surfaces load with their actions", async ({ page }) => {
  await login(page);

  await page.goto("/admin/home-content");
  await expect(page.getByRole("button", { name: "حفظ كمسودة" })).toBeVisible();
  await expect(page.getByRole("button", { name: "نشر الصفحة" })).toBeVisible();

  await page.goto("/admin/content/articles");
  await expect(page.getByRole("heading", { name: "إدارة المقالات" })).toBeVisible();
  await expect(page.getByRole("button", { name: "مقال جديد" })).toBeVisible();

  await page.goto("/admin/content/policies");
  await expect(page.getByRole("heading", { name: "إدارة السياسات" })).toBeVisible();
  await expect(page.getByRole("button", { name: "نسخة جديدة" })).toBeVisible();

  await page.goto("/admin/media");
  await expect(page.getByRole("heading", { name: "مكتبة الصور" })).toBeVisible();
  await expect(page.getByRole("button", { name: "رفع صورة" })).toBeVisible();

  await page.goto("/admin/audit");
  await expect(page.getByRole("heading", { name: "سجل التدقيق الأمني" })).toBeVisible();
});
