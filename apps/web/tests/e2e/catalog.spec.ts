import { execFileSync } from "node:child_process";
import { createHash, randomBytes } from "node:crypto";
import { expect, test } from "@playwright/test";

const container = process.env.E2E_DB_CONTAINER;
const database = process.env.E2E_DB_NAME;
const databaseUser = process.env.E2E_DB_USER;
const previewOwnerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const previewOwnerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const password = "catalog journey password";

test.skip(
  !container || !database || !databaseUser,
  "Catalog E2E requires the isolated PostgreSQL test database.",
);

test.skip(
  true,
  "Legacy catalog-admin journey targets the pre-redesign taxonomy labels. " +
    "Catalog administration is covered by admin-m5; this spec needs a rewrite against the current v2 admin UI.",
);

test("admin creates and publishes a catalog product that the storefront then exposes", async ({ page }) => {
  test.setTimeout(180_000);
  const suffix = `${Date.now()}${Math.floor(Math.random() * 10_000)}`;
  const username = `catalog-${suffix}`;
  const email = `${username}@example.test`;
  const categoryName = `Graphics Cards ${suffix}`;
  const categorySlug = `graphics-cards-${suffix}`;
  const brandName = `GPU Brand ${suffix}`;
  const brandSlug = `gpu-brand-${suffix}`;
  const productName = `RTX Test ${suffix}`;
  const productSlug = `rtx-test-${suffix}`;

  const loginIdentifier = previewOwnerUsername ?? username;
  const loginPassword = previewOwnerPassword ?? password;
  if (!previewOwnerUsername || !previewOwnerPassword) {
    await page.goto("/register");
    await page.getByLabel("الاسم الكامل").fill("Catalog E2E");
    await page.getByLabel("اسم المستخدم").fill(username);
    await page.getByLabel("البريد الإلكتروني").fill(email);
    await page.getByRole("button", { name: "إنشاء الحساب" }).click();
    await expect(page.getByRole("heading", { name: "راجع بريدك الإلكتروني" })).toBeVisible();

    const token = replaceLatestVerificationToken(email);
    await page.goto("about:blank");
    await page.goto(`/verify-email#token=${token}`);
    await page.getByLabel("كلمة المرور الجديدة").fill(password);
    await page.getByLabel("تأكيد كلمة المرور").fill(password);
    await page.getByRole("button", { name: "تأكيد البريد" }).click();
    await expect(page.getByRole("heading", { name: "تم تأكيد حسابك" })).toBeVisible();
    grantCatalogPermissions(email);
  }

  await page.goto("/login");
  await page.getByLabel("اسم المستخدم").fill(loginIdentifier);
  await page.locator('input[autocomplete="current-password"]').fill(loginPassword);
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page).toHaveURL(/\/account\/security$/);

  await page.goto("/admin/catalog");
  await page.getByRole("button", { name: /تصنيف جديد/ }).click();
  const categoryDialog = page.getByRole("dialog", { name: "تصنيف جديد" });
  await categoryDialog.getByLabel("الاسم").fill(categoryName);
  await categoryDialog.getByLabel("Slug").fill(categorySlug);
  const createCategoryResponse = page.waitForResponse((response) =>
    response.request().method() === "POST" && response.url().endsWith("/api/v1/admin/catalog/categories"));
  await categoryDialog.getByRole("button", { name: "حفظ التعديلات" }).click();
  const categoryResponse = await createCategoryResponse;
  expect(categoryResponse.status(), await categoryResponse.text()).toBe(201);
  await expect(page.getByRole("row").filter({ hasText: categoryName })).toBeVisible();

  await page.getByRole("tab", { name: "الماركات" }).click();
  await page.getByRole("button", { name: /ماركة جديدة/ }).click();
  const brandDialog = page.getByRole("dialog", { name: "ماركة جديدة" });
  await brandDialog.getByLabel("الاسم").fill(brandName);
  await brandDialog.getByLabel("Slug").fill(brandSlug);
  await brandDialog.getByRole("button", { name: "حفظ التعديلات" }).click();
  await expect(page.getByRole("row").filter({ hasText: brandName })).toBeVisible();

  await page.locator("#main-content").getByRole("link", { name: "منتج جديد" }).click();
  await expect(page).toHaveURL(/\/admin\/catalog\/products\/new$/);
  await expect(page.getByRole("heading", { name: "منتج جديد" })).toBeVisible();
  await page.getByLabel("الاسم").fill(productName);
  await page.getByLabel("Slug").fill(productSlug);
  await page.getByLabel("SKU").fill(`SKU-${suffix}`);
  await page.getByLabel("التصنيف").selectOption({ label: categoryName });
  await page.getByLabel("الماركة").selectOption({ label: brandName });
  await page.getByLabel("الوصف").fill("A complete catalog integration test product.");
  await page.getByRole("tab", { name: "السعر والنشر" }).click();
  await page.getByLabel("سعر البيع الأساسي").fill("12500");

  await page.getByRole("tab", { name: /المتغيرات/ }).click();
  await page.getByRole("button", { name: "إضافة متغير" }).click();
  await page.getByLabel("اسم المتغير").fill("16 GB");
  await page.getByLabel("SKU").last().fill(`VAR-${suffix}`);
  await page.getByLabel("سعر المتغير").fill("12750");
  await page.getByRole("tab", { name: /المواصفات/ }).click();
  await page.getByRole("button", { name: "إضافة مواصفة" }).click();
  await page.getByLabel("المتغيرات").last().selectOption({ label: "16 GB" });
  await page.getByLabel("المجموعة").fill("Memory");
  await page.getByLabel("الاسم").last().fill("Capacity");
  await page.getByLabel("القيمة").fill("16 GB");
  await page.getByRole("button", { name: "حفظ" }).click();
  await expect(page).toHaveURL(/\/admin\/catalog\/products\/[0-9a-f-]+$/);
  const editorUrl = page.url();
  await page.goto(`${editorUrl}/preview`);
  await expect(page.getByText("معاينة مسودة إدارية · مسودة")).toBeVisible();
  await expect(page.getByRole("heading", { name: productName })).toBeVisible();
  await page.goto(editorUrl);

  await page.goto(`/products?search=${encodeURIComponent(`SKU-${suffix}`)}`);
  await expect(page.getByRole("heading", { name: productName })).toHaveCount(0);

  await page.goBack();
  await page.locator('input[type="file"]').setInputFiles({
    name: "gpu.png",
    mimeType: "image/png",
    buffer: Buffer.from(onePixelPng, "base64"),
  });
  await page.getByLabel("وصف الصورة").fill("Test GPU");
  await page.getByRole("button", { name: "رفع الصورة" }).click();
  await expect(page.getByText("Test GPU")).toBeVisible();

  await page.getByRole("tab", { name: "السعر والنشر" }).click();
  await page.getByLabel("منشور").selectOption("Published");
  const publishResponsePromise = page.waitForResponse((response) =>
    response.request().method() === "PUT" &&
    /\/api\/v1\/admin\/catalog\/products\/[0-9a-f-]+$/.test(response.url()));
  await page.getByRole("button", { name: "حفظ" }).click();
  const publishResponse = await publishResponsePromise;
  expect(publishResponse.status(), await publishResponse.text()).toBe(200);
  await page.goto(`/products?search=${encodeURIComponent(`SKU-${suffix}`)}`);
  await expect(page.getByRole("heading", { name: productName })).toBeVisible();
  await page.getByRole("link", { name: productName }).click();
  await expect(page.getByRole("heading", { name: productName })).toBeVisible();
  await page.getByRole("button", { name: /16 GB/ }).click();
  await expect(page.getByText(`SKU: VAR-${suffix}`)).toBeVisible();
  await expect(page.getByText(/12,?750/).first()).toBeVisible();
  await expect(page.getByText("Capacity")).toBeVisible();
  await expect(page.locator("dd").filter({ hasText: /^16 GB$/ })).toBeVisible();
});

function replaceLatestVerificationToken(email: string): string {
  const rawToken = randomBytes(32).toString("base64url");
  const hash = createHash("sha256").update(Buffer.from(rawToken, "base64url")).digest("hex");
  runSql(`
    UPDATE identity_tokens
    SET token_hash = decode('${hash}', 'hex'),
        created_at_utc = CURRENT_TIMESTAMP,
        expires_at_utc = CURRENT_TIMESTAMP + interval '24 hours',
        used_at_utc = NULL,
        revoked_at_utc = NULL
    WHERE id = (
      SELECT token.id
      FROM identity_tokens token
      INNER JOIN user_accounts account ON account.id = token.user_account_id
      WHERE account.normalized_email = '${email.toUpperCase()}'
        AND token.purpose = 'EmailVerification'
      ORDER BY token.created_at_utc DESC
      LIMIT 1
    );`);
  return rawToken;
}

function grantCatalogPermissions(email: string): void {
  runSql(`
    INSERT INTO role_permissions (role_id, permission_code)
    SELECT DISTINCT user_role.role_id, permission.code
    FROM user_accounts account
    INNER JOIN user_roles user_role ON user_role.user_account_id = account.id
    CROSS JOIN permissions permission
    WHERE account.normalized_email = '${email.toUpperCase()}'
      AND permission.code LIKE 'Catalog.%'
    ON CONFLICT DO NOTHING;`);
}

function runSql(sql: string): void {
  if (!container || !database || !databaseUser) throw new Error("E2E database is not configured.");
  const output = execFileSync("docker", [
    "exec", container, "psql", "-v", "ON_ERROR_STOP=1",
    "-U", databaseUser, "-d", database, "-c", sql,
  ], { encoding: "utf8" });
  if (!/(UPDATE 1|INSERT 0 [1-9])/.test(output)) {
    throw new Error("Catalog E2E database setup did not affect the expected rows.");
  }
}

const onePixelPng =
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
