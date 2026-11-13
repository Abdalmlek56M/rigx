import { expect, test } from "@playwright/test";
import path from "node:path";

const ownerUsername = process.env.E2E_PREVIEW_OWNER_USERNAME;
const ownerPassword = process.env.E2E_PREVIEW_OWNER_PASSWORD;
const enabled = process.env.E2E_ACCOUNT_QA === "1" && Boolean(ownerUsername) && Boolean(ownerPassword);
const baseURL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${process.env.E2E_WEB_PORT ?? "3006"}`;
const reviewDirectory = path.resolve(process.cwd(), "..", "..", "docs", "review", "account-wave-2026-11-09");

test.skip(!enabled, "Account QA requires E2E_ACCOUNT_QA=1 and preview owner credentials.");

test("owner updates profile, manages sessions, and invites a staff user", async ({ browser }) => {
  test.setTimeout(180_000);
  const suffix = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  const email = `staff-${suffix}@example.test`;
  const username = `staff-${suffix}`;

  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  await context.addCookies([
    { name: "hardware-store-locale", value: "en", url: baseURL },
    { name: "hardware-store-theme", value: "dark", url: baseURL },
  ]);
  await context.addInitScript(() => {
    if (!window.localStorage.getItem("hardware-store-theme")) {
      window.localStorage.setItem("hardware-store-theme", "dark");
    }
  });
  const page = await context.newPage();
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error") consoleErrors.push(message.text());
  });
  page.on("pageerror", (error) => consoleErrors.push(String(error)));
  page.on("response", (response) => {
    if (response.status() >= 500) serverErrors.push(`${response.status()} ${response.url()}`);
  });

  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Username").fill(ownerUsername!);
  await page.locator('input[autocomplete="current-password"]').fill(ownerPassword!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await page.waitForURL(/\/account\/security$/, { timeout: 30_000 });

  await page.goto("/account/profile", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Account profile" })).toBeVisible();
  await page.getByLabel("Full name", { exact: true }).fill("Preview Owner QA");
  await page.getByLabel("Phone number", { exact: true }).fill("+201000000111");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Account profile saved.")).toBeVisible();
  await page.screenshot({ path: path.join(reviewDirectory, "account-profile-dark.png"), fullPage: true });

  await page.goto("/account/sessions", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: "Sessions and devices" })).toBeVisible();
  await expect(page.getByText("This session")).toBeVisible();
  await page.getByRole("button", { name: "End other sessions" }).click();
  await expect(page.getByText("Other sessions ended.")).toBeVisible();
  await page.screenshot({ path: path.join(reviewDirectory, "account-sessions-dark.png"), fullPage: true });

  await page.goto("/admin/identity/users", { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { level: 1, name: /users/i })).toBeVisible();
  await page.getByRole("button", { name: "New user" }).click();
  await page.getByLabel("Email address", { exact: true }).fill(email);
  await page.getByLabel("Username", { exact: true }).fill(username);
  await page.getByLabel("Full name", { exact: true }).fill("Staff QA");
  await page.getByLabel("Phone number", { exact: true }).fill("+201000000222");
  await page.locator("fieldset input[type=checkbox]").first().check();
  await page.screenshot({ path: path.join(reviewDirectory, "admin-create-user-dark.png"), fullPage: true });
  await page.getByRole("button", { name: "Create user" }).click();
  await expect(page.getByText(/activation email|created/i)).toBeVisible();
  await page.screenshot({ path: path.join(reviewDirectory, "admin-users-after-create-dark.png"), fullPage: true });

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
  await mobilePage.goto("/account/sessions", { waitUntil: "domcontentloaded" });
  await expect(mobilePage.getByRole("heading", { level: 1, name: "Sessions and devices" })).toBeVisible();
  await mobilePage.screenshot({ path: path.join(reviewDirectory, "account-sessions-mobile-dark.png"), fullPage: true });
  await mobileContext.close();
  await context.close();

  const unexpectedErrors = consoleErrors.filter((message) => !message.includes("401 (Unauthorized)"));
  expect.soft(serverErrors).toEqual([]);
  expect.soft(unexpectedErrors).toEqual([]);
});
