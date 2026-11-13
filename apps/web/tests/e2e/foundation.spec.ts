import { expect, test } from "@playwright/test";

test.skip(!process.env.API_ORIGIN, "Storefront E2E requires the isolated Preview API.");

test("storefront homepage loads real catalog data and persists language and theme", async ({ page }) => {
  await page.goto("/");

  await expect(page).toHaveTitle(/RIGX/i);
  await expect(page.locator("html")).toHaveAttribute("lang", "ar");
  await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
  await expect(
    page.getByRole("heading", { name: /اختار قطعتك/ }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "أحدث المنتجات" })).toBeVisible();
  await expect(page.getByRole("link", { name: /AMD Ryzen 7 9800X3D/ }).first()).toBeVisible();

  await page.getByRole("button", { name: "English" }).click();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.locator("html")).toHaveAttribute("dir", "ltr");
  await expect(
    page.getByRole("heading", { name: /Choose your next/ }),
  ).toBeVisible();

  await page.getByRole("button", { name: "Switch to dark mode" }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");

  await page.reload();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
});

test("storefront carousels keep their controls usable in RTL and LTR", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");

  const categories = page.getByRole("region", { name: "تسوق حسب القسم" });
  await expect(categories).toBeVisible();
  await expect(categories).toHaveAttribute("aria-roledescription", "carousel");
  await expect(categories).toHaveAttribute("data-can-scroll", "true");

  const viewport = categories.getByRole("group", { name: "تسوق حسب القسم" });
  await viewport.focus();
  await page.keyboard.press("ArrowLeft");
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("Home");

  const next = categories.getByRole("button", { name: "العناصر التالية" });
  if (await next.isEnabled()) await next.click();
  await page.waitForTimeout(300);
  await expect(page).toHaveURL(/\/$/);

  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.getByRole("button", { name: "English" }).click();
  const englishCategories = page.getByRole("region", { name: "Shop by category" });
  await expect(englishCategories).toBeVisible();
  const englishViewport = englishCategories.getByRole("group", { name: "Shop by category" });
  await englishViewport.focus();
  await page.keyboard.press("ArrowRight");
  await page.keyboard.press("ArrowLeft");
  await expect(page).toHaveURL(/\/$/);
});

test("mega menu and mobile navigation close on Escape, outside click, and route change", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/");
  await page.getByRole("button", { name: "English" }).click();

  const trigger = page.getByRole("button", { name: "All departments" });
  const menu = page.getByRole("region", { name: "All departments" });
  await trigger.click();
  await expect(menu).toBeVisible();
  await expect(trigger).toHaveAttribute("aria-expanded", "true");

  await page.keyboard.press("Escape");
  await expect(menu).toHaveCount(0);
  await expect(trigger).toBeFocused();

  await trigger.click();
  await expect(menu).toBeVisible();
  await page.locator("main").click({ position: { x: 8, y: 8 } });
  await expect(menu).toHaveCount(0);

  await trigger.click();
  await expect(menu).toBeVisible();
  await menu.getByRole("link").first().click();
  await expect(menu).toHaveCount(0);
  await expect(page).toHaveURL(/\/products\?category=/);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  const drawerTrigger = page.getByRole("button", { name: "Menu", exact: true });
  await drawerTrigger.click();
  const drawer = page.getByRole("dialog", { name: "Menu" });
  await expect(drawer).toBeVisible();
  await expect(drawerTrigger).toHaveAttribute("aria-expanded", "true");
  await page.keyboard.press("Escape");
  await expect(drawer).toHaveCount(0);
  await expect(drawerTrigger).toBeFocused();
});
