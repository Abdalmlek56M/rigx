import { expect, type APIRequestContext, type Page } from "@playwright/test";
import { latestCode } from "./api";
import { ar } from "./copy";

const ARRIVAL_MS = 10_000;
/** The API sends a new code at most once a minute; a request inside that window is answered without sending one. */
const RESEND_WAIT_MS = 70_000;

/** Asks the login page (it sends the person on to `next`) for a code and returns it once the dev endpoint shows it (the email is sent a moment later). */
export async function requestCode(page: Page, api: APIRequestContext, email: string, next = "/admin"): Promise<string> {
  const before = await latestCode(api, email);
  await page.goto(`/ar/login?next=${next}`);
  await page.getByLabel(ar.auth.email.label).fill(email);
  await page.getByRole("button", { name: ar.auth.email.send }).click();
  await expect(page.getByRole("heading", { name: ar.auth.code.title })).toBeVisible();

  let fresh: string | undefined;
  const arrived = async (timeout: number) => {
    await expect
      .poll(async () => {
        const current = await latestCode(api, email);
        fresh = current && current.expiresAtUtc !== before?.expiresAtUtc ? current.code : undefined;
        return fresh;
      }, { timeout })
      .toBeDefined();
  };

  try {
    await arrived(ARRIVAL_MS);
  } catch {
    // A code was sent less than a minute ago (an earlier run): wait for "send a new code" and ask again.
    await page.getByRole("button", { name: ar.auth.code.resend }).click({ timeout: RESEND_WAIT_MS });
    await arrived(ARRIVAL_MS);
  }
  return fresh ?? "";
}

export async function enterCode(page: Page, code: string): Promise<void> {
  await page.getByLabel(ar.auth.code.label).fill(code);
  await page.getByRole("button", { name: ar.auth.code.submit, exact: true }).click();
}

/** Signs in through the real login page (new addresses get a new customer account). */
export async function signIn(page: Page, api: APIRequestContext, email: string, next = "/admin"): Promise<void> {
  await enterCode(page, await requestCode(page, api, email, next));
}
