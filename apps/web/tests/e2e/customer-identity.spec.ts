import { execFileSync } from "node:child_process";
import { createHash, randomBytes } from "node:crypto";
import { expect, test } from "@playwright/test";

const container = process.env.E2E_DB_CONTAINER;
const database = process.env.E2E_DB_NAME;
const databaseUser = process.env.E2E_DB_USER;
const initialPassword = "customer journey password";
const changedPassword = "customer changed password";
const resetPasswordValue = "customer recovered password";

test.skip(
  !container || !database || !databaseUser,
  "E2E database helper variables are required to deliver fake email tokens.",
);

test("customer completes registration, verification, login, password change, logout, and reset", async ({ browser, page }) => {
  test.setTimeout(120_000);

  const suffix = `${Date.now()}${Math.floor(Math.random() * 10_000)}`;
  const username = `e2e-${suffix}`;
  const email = `${username}@example.test`;

  await page.goto("/register");
  await page.getByLabel("الاسم الكامل").fill("E2E Customer");
  await page.getByLabel("اسم المستخدم").fill(username);
  await page.getByLabel("البريد الإلكتروني").fill(email);
  await page.getByRole("button", { name: "إنشاء الحساب" }).click();
  await expect(page.getByRole("heading", { name: "راجع بريدك الإلكتروني" })).toBeVisible();

  const verificationToken = replaceLatestToken(email, "EmailVerification", 24);
  await page.goto("about:blank");
  await page.goto(`/verify-email#token=${verificationToken}`);
  await expect(page).not.toHaveURL(/#token=/);
  await page.getByLabel("كلمة المرور الجديدة").fill(initialPassword);
  await page.getByLabel("تأكيد كلمة المرور").fill(initialPassword);
  await page.getByRole("button", { name: "تأكيد البريد" }).click();
  await expect(page.getByRole("heading", { name: "تم تأكيد حسابك" })).toBeVisible();

  await page.goto("about:blank");
  await page.goto(`/verify-email#token=${verificationToken}`);
  await page.getByLabel("كلمة المرور الجديدة").fill(initialPassword);
  await page.getByLabel("تأكيد كلمة المرور").fill(initialPassword);
  await page.getByRole("button", { name: "تأكيد البريد" }).click();
  await expect(page.getByText("الرابط غير صالح أو انتهت صلاحيته أو تم استخدامه سابقًا.")).toBeVisible();

  await page.getByRole("link", { name: "تسجيل الدخول" }).first().click();
  await page.getByLabel("اسم المستخدم").fill(username);
  await page.locator('input[autocomplete="current-password"]').fill("wrong customer password");
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page.getByText("بيانات الدخول غير صحيحة أو الحساب غير جاهز.")).toBeVisible();
  await page.locator('input[autocomplete="current-password"]').fill(initialPassword);
  const customerLogin = page.getByRole("button", { name: "دخول", exact: true });
  await customerLogin.click();
  try {
    await page.waitForURL(/\/account\/security$/, { timeout: 8000 });
  } catch {
    await customerLogin.click();
    await page.waitForURL(/\/account\/security$/, { timeout: 15000 });
  }
  await expect(page.getByRole("heading", { name: "حسابك" })).toBeVisible();
  await expect(page.locator("#main-content").getByText("E2E Customer", { exact: true })).toBeVisible();

  const staleContext = await browser.newContext({
    baseURL: test.info().project.use.baseURL,
  });
  const stalePage = await staleContext.newPage();
  await stalePage.goto("/login");
  await stalePage.getByLabel("اسم المستخدم").fill(username);
  await stalePage.locator('input[autocomplete="current-password"]').fill(initialPassword);
  await stalePage.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(stalePage).toHaveURL(/\/account\/security$/);

  await page.getByLabel("كلمة المرور الحالية").fill(initialPassword);
  await page.getByLabel("كلمة المرور الجديدة").fill(changedPassword);
  await page.getByLabel("تأكيد كلمة المرور").fill(changedPassword);
  await page.getByRole("button", { name: "تغيير كلمة المرور" }).click();
  await expect(page.getByRole("heading", { name: "تم تغيير كلمة المرور" })).toBeVisible();

  await stalePage.goto("/account/security");
  await expect(stalePage).toHaveURL(/\/login\?notice=session-expired&returnTo=%2Faccount%2Fsecurity$/);
  await expect(stalePage.getByText("انتهت جلستك أو تم إبطالها. سجّل الدخول من جديد.")).toBeVisible();
  await staleContext.close();

  await page.goto("/login?notice=password-changed");
  await expect(page.getByText("تم تغيير كلمة المرور. سجّل الدخول من جديد.")).toBeVisible();
  await page.getByLabel("اسم المستخدم").fill(username);
  await page.locator('input[autocomplete="current-password"]').fill(changedPassword);
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page).toHaveURL(/\/account\/security$/);

  await page.getByRole("button", { name: "تسجيل الخروج من هذا الجهاز" }).click();
  await expect(page).toHaveURL(/\/login\?notice=signed-out/);
  await expect(page.getByText("تم تسجيل خروج الجلسة الحالية.")).toBeVisible();

  await page.goto("/forgot-password");
  await page.getByLabel("البريد الإلكتروني").fill(email);
  await page.getByRole("button", { name: "إرسال تعليمات الاستعادة" }).click();
  await expect(page.getByText(/إذا كان الحساب مؤهلًا فستصلك رسالة الاستعادة/)).toBeVisible();

  const resetToken = replaceLatestToken(email, "PasswordReset", 0.5);
  await page.goto(`/reset-password#token=${resetToken}`);
  await expect(page).not.toHaveURL(/#token=/);
  await page.getByLabel("كلمة المرور الجديدة").fill(resetPasswordValue);
  await page.getByLabel("تأكيد كلمة المرور").fill(resetPasswordValue);
  await page.getByRole("button", { name: "حفظ كلمة المرور" }).click();
  await expect(page.getByRole("heading", { name: "تم تغيير كلمة المرور" })).toBeVisible();

  await page.getByRole("link", { name: "تسجيل الدخول" }).first().click();
  await page.getByLabel("اسم المستخدم").fill(username);
  await page.locator('input[autocomplete="current-password"]').fill(resetPasswordValue);
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page).toHaveURL(/\/account\/security$/);
  await page.goto("/admin");
  await expect(page.getByText("هذا الحساب لا يملك صلاحية الوصول إلى مساحة الإدارة.")).toBeVisible();
  const denied = await page.request.get("/api/v1/admin/catalog/products");
  expect(denied.status()).toBe(403);
  await page.goto("/account/security");
  await page.getByRole("button", { name: "تسجيل الخروج من جميع الأجهزة" }).click();
  await expect(page).toHaveURL(/\/login\?notice=signed-out-all/);
  await expect(page.getByText("تم تسجيل الخروج من جميع الجلسات.")).toBeVisible();
});

function replaceLatestToken(email: string, purpose: string, lifetimeHours: number): string {
  if (!container || !database || !databaseUser) {
    throw new Error("E2E database helper variables are not configured.");
  }

  const rawToken = randomBytes(32).toString("base64url");
  const tokenHash = createHash("sha256").update(Buffer.from(rawToken, "base64url")).digest("hex");
  const normalizedEmail = email.trim().toUpperCase();
  const interval = `${lifetimeHours} hours`;
  const sql = `
    UPDATE identity_tokens
    SET token_hash = decode('${tokenHash}', 'hex'),
        created_at_utc = CURRENT_TIMESTAMP,
        expires_at_utc = CURRENT_TIMESTAMP + interval '${interval}',
        used_at_utc = NULL,
        revoked_at_utc = NULL
    WHERE id = (
      SELECT token.id
      FROM identity_tokens AS token
      INNER JOIN user_accounts AS account ON account.id = token.user_account_id
      WHERE account.normalized_email = '${normalizedEmail.replaceAll("'", "''")}'
        AND token.purpose = '${purpose.replaceAll("'", "''")}'
      ORDER BY token.created_at_utc DESC
      LIMIT 1
    );`;
  const output = execFileSync(
    "docker",
    [
      "exec",
      container,
      "psql",
      "-v",
      "ON_ERROR_STOP=1",
      "-U",
      databaseUser,
      "-d",
      database,
      "-c",
      sql,
    ],
    { encoding: "utf8" },
  );
  if (!output.includes("UPDATE 1")) {
    throw new Error("The test token replacement did not update exactly one row.");
  }
  return rawToken;
}
