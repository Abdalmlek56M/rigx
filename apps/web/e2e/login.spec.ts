import { mkdirSync } from "node:fs";
import { dirname } from "node:path";
import { expect, test } from "@playwright/test";
import { newApi } from "./support/api";
import { ar } from "./support/copy";
import { OWNER_EMAIL, OWNER_STATE } from "./support/env";
import { enterCode, requestCode } from "./support/sign-in";

test("a wrong code is refused, then the owner signs in and sees the dashboard", async ({ page }) => {
  test.setTimeout(150_000);
  const api = await newApi();
  const code = await requestCode(page, api, OWNER_EMAIL);

  await enterCode(page, code === "000000" ? "111111" : "000000");
  await expect(page.getByRole("alert").filter({ hasText: /^الكود مش صح/ })).toBeVisible();
  await expect(page).toHaveURL(/\/login/);

  await enterCode(page, code);
  await expect(page).toHaveURL(/\/admin$/);
  await expect(page.getByRole("heading", { name: ar.admin.dashboard.title })).toBeVisible();

  // The other specs start signed in as the owner.
  mkdirSync(dirname(OWNER_STATE), { recursive: true });
  await page.context().storageState({ path: OWNER_STATE });
  await api.dispose();
});
