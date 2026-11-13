import { expect, test } from "@playwright/test";

test.skip(!process.env.API_ORIGIN, "Storefront E2E requires the isolated Preview API.");

test("catalog search, filters, six sort modes, technical facets, and pagination use the backend", async ({ page }) => {
  await page.goto("/products");
  await expect(page.getByRole("heading", { name: "المنتجات" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "الفلاتر" })).toBeVisible();

  const sort = page.getByLabel("الترتيب");
  await expect(sort.locator("option")).toHaveCount(6);
  await expect(sort.locator("option")).toHaveText([
    "الأحدث",
    "الأقدم",
    "السعر: الأقل أولًا",
    "السعر: الأعلى أولًا",
    "الاسم: من A إلى Z",
    "الاسم: من Z إلى A",
  ]);

  await page.locator("#catalog-search").fill("GPU-5080");
  await page.locator("#catalog-search").press("Enter");
  await expect(page).toHaveURL(/search=GPU-5080/);
  await expect(page.getByRole("heading", { name: "NVIDIA GeForce RTX 5080 16GB" })).toBeVisible();

  await page.getByRole("button", { name: "مسح كل الفلاتر" }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.getByRole("radio", { name: "كروت الشاشة" }).check();
  await expect(page).toHaveURL(/category=graphics-cards/);
  await page.getByRole("radio", { name: "NVIDIA" }).check();
  await expect(page).toHaveURL(/brand=nvidia/);
  await expect(page.getByRole("heading", { name: "NVIDIA GeForce RTX 5080 16GB" })).toBeVisible();

  await page.getByRole("button", { name: "مسح كل الفلاتر" }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.getByLabel("أقل سعر").fill("50000");
  await page.getByLabel("أعلى سعر").fill("60000");
  await page.getByRole("button", { name: "تطبيق السعر" }).click();
  await expect(page).toHaveURL(/minimumPrice=50000/);
  await expect(page).toHaveURL(/maximumPrice=60000/);
  await expect(page.getByRole("heading", { name: "NVIDIA GeForce RTX 5080 16GB" })).toBeVisible();

  await page.getByRole("button", { name: "مسح كل الفلاتر" }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.getByRole("radio", { name: "كروت الشاشة" }).check();
  await expect(page).toHaveURL(/category=graphics-cards/);
  await page.getByRole("checkbox", { name: "16 GB GDDR7" }).check();
  await expect(page).toHaveURL(/specification=Graphics%7CMemory%7C16\+GB\+GDDR7/);
  await expect(page.getByRole("heading", { name: "NVIDIA GeForce RTX 5080 16GB" })).toBeVisible();

  await page.getByRole("button", { name: "مسح كل الفلاتر" }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.getByLabel("أقل سعر").fill("0");
  await page.getByRole("button", { name: "تطبيق السعر" }).click();
  await page.getByRole("button", { name: "التالي" }).click();
  await expect(page).toHaveURL(/minimumPrice=0/);
  await expect(page).toHaveURL(/page=2/);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/products");
  const filterTrigger = page.getByRole("button", { name: /^الفلاتر/ });
  await filterTrigger.click();
  const filterDrawer = page.getByRole("dialog", { name: "الفلاتر" });
  await expect(filterDrawer).toBeVisible();
  await filterDrawer.getByRole("radio", { name: "كروت الشاشة" }).check();
  await filterDrawer.getByRole("checkbox", { name: "16 GB GDDR7" }).check();
  await filterDrawer.getByRole("button", { name: "تطبيق", exact: true }).click();
  await expect(filterDrawer).toHaveCount(0);
  await expect(page).toHaveURL(/category=graphics-cards/);
  await expect(page).toHaveURL(/specification=Graphics%7CMemory%7C16\+GB\+GDDR7/);

  await filterTrigger.click();
  await expect(page.getByRole("dialog", { name: "الفلاتر" })).toBeVisible();
  await page.getByRole("button", { name: "إعادة ضبط" }).click();
  await expect(page).toHaveURL(/\/products$/);
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: "الفلاتر" })).toHaveCount(0);
});

test("product details update price, SKU, and specifications with the selected variant", async ({ page }) => {
  await page.goto("/products/amd-radeon-rx-9070-xt-16gb");

  await expect(page.getByRole("heading", { name: "AMD Radeon RX 9070 XT 16GB" })).toBeVisible();
  await page.getByRole("button", { name: /OC Edition/ }).click();
  await expect(page.getByText("SKU: GPU-9070XT-OC")).toBeVisible();
  await expect(page.getByText(/40,?990/).first()).toBeVisible();
  await expect(page.getByText("PCIe 5.0")).toBeVisible();
  await page.getByRole("button", { name: "فتح الصورة" }).click();
  await expect(page.getByRole("dialog", { name: "AMD Radeon RX 9070 XT 16GB" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: "AMD Radeon RX 9070 XT 16GB" })).toHaveCount(0);

  await page.goto("/products?search=MON-TUF27");
  await expect(page.getByText("لا توجد منتجات منشورة مطابقة.")).toBeVisible();
  await expect(page.getByRole("heading", { name: "ASUS TUF Gaming 27 QHD" })).toHaveCount(0);
});

test("login posts credentials without exposing them in the URL and password visibility is keyboard accessible", async ({ page }) => {
  const username = process.env.E2E_PREVIEW_OWNER_USERNAME;
  const password = process.env.E2E_PREVIEW_OWNER_PASSWORD;
  test.skip(!username || !password, "Preview owner credentials are required for the login security flow.");

  let submittedBody: Record<string, string> | null = null;
  page.on("request", (request) => {
    if (request.method() === "POST" && request.url().endsWith("/api/v1/identity/sessions")) {
      submittedBody = request.postDataJSON() as Record<string, string>;
    }
  });

  await page.goto("/admin");
  await expect(page).toHaveURL(/\/login\?returnTo=%2Fadmin$/);
  await page.waitForTimeout(800);
  const passwordInput = page.locator('input[autocomplete="current-password"]');
  await page.getByLabel("اسم المستخدم").fill(username!);
  await passwordInput.fill("definitely wrong preview password");
  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page.getByText("بيانات الدخول غير صحيحة أو الحساب غير جاهز.")).toBeVisible();
  expect(new URL(page.url()).search).not.toContain("password");

  await passwordInput.fill(password!);

  await page.getByRole("button", { name: "إظهار كلمة المرور" }).click();
  await expect(passwordInput).toHaveAttribute("type", "text");
  await expect(passwordInput).toBeFocused();
  await page.getByRole("button", { name: "إخفاء كلمة المرور" }).click();

  await page.getByRole("button", { name: "دخول", exact: true }).click();
  await expect(page).toHaveURL(/\/admin$/);
  await page.waitForLoadState("networkidle");
  expect(new URL(page.url()).search).toBe("");
  expect(submittedBody).toMatchObject({ identifier: username, password });

  await expect(page.getByRole("heading", { name: "لوحة التحكم" })).toBeVisible();
  const productsResponse = await page.request.get("/api/v1/admin/catalog/products");
  expect(productsResponse.ok()).toBe(true);
  const products = await productsResponse.json() as Array<{ status: string }>;
  const publishedCount = products.filter((product) => product.status === "Published").length;
  await expect(page.getByLabel(`منتجات منشورة: ${publishedCount}`)).toBeVisible();
  const categoriesResponse = await page.request.get("/api/v1/admin/catalog/categories");
  const categories = await categoriesResponse.json() as Array<{ isActive: boolean; children: unknown[] }>;
  const flattenCategories = (items: typeof categories): typeof categories =>
    items.flatMap((category) => [category, ...flattenCategories(category.children as typeof categories)]);
  const activeCategoryCount = flattenCategories(categories).filter((category) => category.isActive).length;
  await expect(page.getByLabel(`أقسام نشطة: ${activeCategoryCount}`)).toBeVisible();
  const brandsResponse = await page.request.get("/api/v1/admin/catalog/brands");
  const brands = await brandsResponse.json() as Array<{ isActive: boolean }>;
  const activeBrandCount = brands.filter((brand) => brand.isActive).length;
  await expect(page.getByLabel(`علامات تجارية نشطة: ${activeBrandCount}`)).toBeVisible();

  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole("button", { name: "فتح قائمة الإدارة" }).click();
  await expect(page.getByRole("dialog", { name: "فتح قائمة الإدارة" })).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog", { name: "فتح قائمة الإدارة" })).toHaveCount(0);
  await page.setViewportSize({ width: 1440, height: 1000 });

  await page.getByRole("link", { name: "إدارة الكتالوج" }).first().click();
  await expect(page).toHaveURL(/\/admin\/catalog$/);
  await expect(page.getByRole("heading", { name: "إدارة الكتالوج" })).toBeVisible();

  await page.locator("header summary").click();
  await page.getByRole("button", { name: "تسجيل الخروج" }).click();
  await expect(page).toHaveURL(/\/login\?notice=signed-out$/);
});
