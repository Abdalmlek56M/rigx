"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { type FormEvent, useEffect, useState } from "react";
import {
  ApiError,
  catalogMediaUrl,
  cleanupCatalogMedia,
  deleteBrandLogo,
  deleteCategoryImage,
  getAdminBrands,
  getAdminCategories,
  getAdminProducts,
  getCatalogMediaAudit,
  saveBrand,
  saveCategory,
  setCatalogProductStatus,
  uploadBrandLogo,
  uploadCategoryImage,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogMediaAudit,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import {
  ConfirmDialog,
  FormStack,
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  TextField,
} from "@/components/ui";

function flatten(categories: CatalogCategory[], depth = 0): Array<CatalogCategory & { depth: number }> {
  return categories.flatMap((category) => [
    { ...category, depth },
    ...flatten(category.children, depth + 1),
  ]);
}

export function CatalogAdmin() {
  const { messages } = useLocale();
  const router = useRouter();
  const { status, user } = useSession();
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [categoryName, setCategoryName] = useState("");
  const [categorySlug, setCategorySlug] = useState("");
  const [categoryParent, setCategoryParent] = useState("");
  const [categoryDescription, setCategoryDescription] = useState("");
  const [categoryActive, setCategoryActive] = useState(true);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [brandName, setBrandName] = useState("");
  const [brandSlug, setBrandSlug] = useState("");
  const [brandDescription, setBrandDescription] = useState("");
  const [brandSupportUrl, setBrandSupportUrl] = useState("");
  const [brandActive, setBrandActive] = useState(true);
  const [editingBrandId, setEditingBrandId] = useState<string | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [mediaAudit, setMediaAudit] = useState<CatalogMediaAudit | null>(null);
  const [showInactiveTaxonomy, setShowInactiveTaxonomy] = useState(false);
  const [productStatusFilter, setProductStatusFilter] = useState<"Published" | "Draft" | "All">("Published");
  const [cleanupConfirmationOpen, setCleanupConfirmationOpen] = useState(false);

  const mayManage = user?.permissions.some((permission) => permission.startsWith("Catalog.")) ?? false;

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [categoryData, brandData, productData] = await Promise.all([
        getAdminCategories(),
        getAdminBrands(),
        getAdminProducts(),
      ]);
      setCategories(categoryData);
      setBrands(brandData);
      setProducts(productData);
    } catch (requestError) {
      setError(requestError instanceof ApiError && requestError.status === 403
        ? messages.adminCatalogForbidden
        : apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (status === "anonymous") {
      router.replace("/login?returnTo=%2Fadmin%2Fcatalog");
      return;
    }
    if (status === "authenticated" && mayManage) void load();
    if (status === "authenticated" && !mayManage) setLoading(false);
  }, [mayManage, router, status]);

  async function createCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending("category");
    setError(null);
    try {
      await saveCategory(editingCategoryId, {
        name: categoryName,
        slug: categorySlug,
        parentId: categoryParent || null,
        description: categoryDescription || undefined,
        sortOrder: 0,
        isActive: categoryActive,
        rowVersion: editingCategoryId
          ? categories.find((category) => category.id === editingCategoryId)?.rowVersion
          : undefined,
      });
      resetCategoryForm();
      setNotice(messages.adminProductSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function createBrand(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending("brand");
    setError(null);
    try {
      await saveBrand(editingBrandId, {
        name: brandName,
        slug: brandSlug,
        description: brandDescription || undefined,
        supportUrl: brandSupportUrl || undefined,
        isActive: brandActive,
        rowVersion: editingBrandId
          ? brands.find((brand) => brand.id === editingBrandId)?.rowVersion
          : undefined,
      });
      resetBrandForm();
      setNotice(messages.adminProductSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function toggleStatus(product: CatalogProductSummary) {
    setPending(product.id);
    setError(null);
    try {
      const result = await setCatalogProductStatus(
        product.id,
        product.status === "Published" ? "Draft" : "Published",
        await getRowVersion(product.id),
      );
      setProducts((items) => items.map((item) =>
        item.id === product.id ? { ...item, status: result.status } : item));
      setNotice(messages.adminProductSaved);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function toggleCategory(category: CatalogCategory) {
    setPending(category.id);
    setError(null);
    try {
      await saveCategory(category.id, {
        name: category.name,
        slug: category.slug,
        parentId: category.parentId,
        description: category.description ?? undefined,
        iconStorageKey: category.iconStorageKey ?? undefined,
        sortOrder: category.sortOrder,
        isActive: !category.isActive,
        rowVersion: category.rowVersion,
      });
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function uploadCategoryAsset(categoryId: string, file: File | null) {
    if (!file) return;
    setPending(categoryId);
    setError(null);
    try {
      await uploadCategoryImage(categoryId, file);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function removeCategoryAsset(categoryId: string) {
    setPending(categoryId);
    try {
      await deleteCategoryImage(categoryId);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function uploadBrandAsset(brandId: string, file: File | null) {
    if (!file) return;
    setPending(brandId);
    setError(null);
    try {
      await uploadBrandLogo(brandId, file);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function removeBrandAsset(brandId: string) {
    setPending(brandId);
    try {
      await deleteBrandLogo(brandId);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function auditMedia(cleanup: boolean) {
    setPending(cleanup ? "media-cleanup" : "media-audit");
    setError(null);
    try {
      const result = cleanup ? (await cleanupCatalogMedia()).audit : await getCatalogMediaAudit();
      setMediaAudit(result);
      setNotice(cleanup ? messages.adminCleanupMedia : messages.adminAuditMedia);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function toggleBrand(brand: CatalogBrand) {
    setPending(brand.id);
    setError(null);
    try {
      await saveBrand(brand.id, {
        name: brand.name,
        slug: brand.slug,
        description: brand.description ?? undefined,
        logoStorageKey: brand.logoStorageKey ?? undefined,
        supportUrl: brand.supportUrl ?? undefined,
        isActive: !brand.isActive,
        rowVersion: brand.rowVersion,
      });
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  function editCategory(category: CatalogCategory) {
    setEditingCategoryId(category.id);
    setCategoryName(category.name);
    setCategorySlug(category.slug);
    setCategoryParent(category.parentId ?? "");
    setCategoryDescription(category.description ?? "");
    setCategoryActive(category.isActive);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function editBrand(brand: CatalogBrand) {
    setEditingBrandId(brand.id);
    setBrandName(brand.name);
    setBrandSlug(brand.slug);
    setBrandDescription(brand.description ?? "");
    setBrandSupportUrl(brand.supportUrl ?? "");
    setBrandActive(brand.isActive);
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function resetCategoryForm() {
    setEditingCategoryId(null);
    setCategoryName("");
    setCategorySlug("");
    setCategoryParent("");
    setCategoryDescription("");
    setCategoryActive(true);
  }

  function resetBrandForm() {
    setEditingBrandId(null);
    setBrandName("");
    setBrandSlug("");
    setBrandDescription("");
    setBrandSupportUrl("");
    setBrandActive(true);
  }

  if (status === "loading" || loading) return <LoadingCard />;
  if (status === "authenticated" && !mayManage) {
    return <main className="mx-auto max-w-3xl px-5 py-16"><Message tone="error">{messages.adminCatalogForbidden}</Message></main>;
  }

  const flatCategories = flatten(categories);
  const visibleCategories = flatCategories.filter((category) => showInactiveTaxonomy || category.isActive);
  const visibleBrands = brands.filter((brand) => showInactiveTaxonomy || brand.isActive);
  const visibleProducts = products.filter((product) =>
    productStatusFilter === "All" || product.status === productStatusFilter);
  return (
    <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-16">
      <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
        <PageIntro eyebrow={messages.adminCatalogEyebrow} title={messages.adminCatalogTitle} lead={messages.adminCatalogLead} />
        <Link href="/admin/catalog/products/new" className="rounded-xl bg-blue-700 px-5 py-3 font-bold text-white">
          {messages.adminNewProduct}
        </Link>
      </div>
      {notice ? <div className="mb-5"><Message tone="success">{notice}</Message></div> : null}
      {error ? <div className="mb-5"><Message tone="error">{error}</Message></div> : null}

      <div className="grid gap-6 lg:grid-cols-2">
        <section id="categories" className="scroll-mt-24 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-bold">{messages.adminCategories}</h2>
            <label className="flex items-center gap-2 text-xs font-semibold text-[var(--ds-text-muted)]">
              <input type="checkbox" checked={showInactiveTaxonomy} onChange={(event) => setShowInactiveTaxonomy(event.target.checked)} />
              {messages.adminShowInactive}
            </label>
          </div>
          <ul className="mt-4 grid gap-2">
            {visibleCategories.map((category) => (
              <li key={category.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-[var(--ds-surface-muted)] px-3 py-2" style={{ marginInlineStart: `${category.depth}rem` }}>
                <span>
                  <span className="font-semibold">{category.name}</span>
                  <span className={`ms-2 text-xs font-bold ${category.isEffectivelyActive ? "text-emerald-700" : "text-amber-700"}`}>
                    {category.isEffectivelyActive ? messages.adminActive : messages.adminInactive}
                  </span>
                </span>
                <span className="flex items-center gap-2">
                  {category.iconStorageKey ? (
                    <img src={catalogMediaUrl(category.iconStorageKey)} alt="" className="h-8 w-8 rounded object-contain" />
                  ) : null}
                  <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{category.slug}</span>
                  <button type="button" onClick={() => editCategory(category)} className="text-xs font-bold text-blue-700">{messages.adminEdit}</button>
                  <button type="button" onClick={() => void toggleCategory(category)} disabled={pending === category.id} className="text-xs font-bold text-[var(--ds-text)]">
                    {category.isActive ? messages.adminDisable : messages.adminEnable}
                  </button>
                  <label className="cursor-pointer text-xs font-bold text-blue-700">
                    {messages.adminCategoryImage}
                    <input type="file" accept="image/*" className="sr-only" onChange={(event) => void uploadCategoryAsset(category.id, event.target.files?.[0] ?? null)} />
                  </label>
                  {category.iconStorageKey ? <button type="button" onClick={() => void removeCategoryAsset(category.id)} className="text-xs font-bold text-red-700">{messages.adminRemove}</button> : null}
                </span>
              </li>
            ))}
          </ul>
          <form onSubmit={createCategory} className="mt-5">
            <FormStack>
              <TextField label={messages.adminName} value={categoryName} onChange={(event) => setCategoryName(event.target.value)} required />
              <TextField label={messages.adminSlug} value={categorySlug} onChange={(event) => setCategorySlug(event.target.value)} dir="ltr" required />
              <TextField label={messages.adminDescription} value={categoryDescription} onChange={(event) => setCategoryDescription(event.target.value)} />
              <label className="text-sm font-semibold">
                {messages.adminParentCategory}
                <select value={categoryParent} onChange={(event) => setCategoryParent(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-slate-300 bg-[var(--ds-surface)] px-3">
                  <option value="">—</option>
                  {flatCategories.filter((category) => category.id !== editingCategoryId).map((category) => <option key={category.id} value={category.id}>{"—".repeat(category.depth)} {category.name}</option>)}
                </select>
              </label>
              <label className="flex items-center gap-2 text-sm font-semibold">
                <input type="checkbox" checked={categoryActive} onChange={(event) => setCategoryActive(event.target.checked)} />
                {messages.adminActive}
              </label>
              <div className="flex gap-2">
                <PrimaryButton type="submit" pending={pending === "category"}>
                  {editingCategoryId ? messages.adminSaveChanges : messages.adminCreateCategory}
                </PrimaryButton>
                {editingCategoryId ? <SecondaryButton type="button" onClick={resetCategoryForm}>{messages.adminCancel}</SecondaryButton> : null}
              </div>
            </FormStack>
          </form>
        </section>

        <section id="brands" className="scroll-mt-24 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <h2 className="text-xl font-bold">{messages.adminBrands}</h2>
            <label className="flex items-center gap-2 text-xs font-semibold text-[var(--ds-text-muted)]">
              <input type="checkbox" checked={showInactiveTaxonomy} onChange={(event) => setShowInactiveTaxonomy(event.target.checked)} />
              {messages.adminShowInactive}
            </label>
          </div>
          <ul className="mt-4 grid gap-2">
            {visibleBrands.map((brand) => (
              <li key={brand.id} className="flex flex-wrap items-center justify-between gap-2 rounded-xl bg-[var(--ds-surface-muted)] px-3 py-2">
                <span>
                  <span className="font-semibold">{brand.name}</span>
                  <span className={`ms-2 text-xs font-bold ${brand.isActive ? "text-emerald-700" : "text-amber-700"}`}>
                    {brand.isActive ? messages.adminActive : messages.adminInactive}
                  </span>
                </span>
                <span className="flex items-center gap-2">
                  {brand.logoStorageKey ? (
                    <img src={catalogMediaUrl(brand.logoStorageKey)} alt="" className="h-8 w-8 rounded object-contain" />
                  ) : null}
                  <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{brand.slug}</span>
                  <button type="button" onClick={() => editBrand(brand)} className="text-xs font-bold text-blue-700">{messages.adminEdit}</button>
                  <button type="button" onClick={() => void toggleBrand(brand)} disabled={pending === brand.id} className="text-xs font-bold text-[var(--ds-text)]">
                    {brand.isActive ? messages.adminDisable : messages.adminEnable}
                  </button>
                  <label className="cursor-pointer text-xs font-bold text-blue-700">
                    {messages.adminBrandLogo}
                    <input type="file" accept="image/*" className="sr-only" onChange={(event) => void uploadBrandAsset(brand.id, event.target.files?.[0] ?? null)} />
                  </label>
                  {brand.logoStorageKey ? <button type="button" onClick={() => void removeBrandAsset(brand.id)} className="text-xs font-bold text-red-700">{messages.adminRemove}</button> : null}
                </span>
              </li>
            ))}
          </ul>
          <form onSubmit={createBrand} className="mt-5">
            <FormStack>
              <TextField label={messages.adminName} value={brandName} onChange={(event) => setBrandName(event.target.value)} required />
              <TextField label={messages.adminSlug} value={brandSlug} onChange={(event) => setBrandSlug(event.target.value)} dir="ltr" required />
              <TextField label={messages.adminDescription} value={brandDescription} onChange={(event) => setBrandDescription(event.target.value)} />
              <TextField label={messages.adminSupportUrl} value={brandSupportUrl} onChange={(event) => setBrandSupportUrl(event.target.value)} dir="ltr" type="url" />
              <label className="flex items-center gap-2 text-sm font-semibold">
                <input type="checkbox" checked={brandActive} onChange={(event) => setBrandActive(event.target.checked)} />
                {messages.adminActive}
              </label>
              <div className="flex gap-2">
                <PrimaryButton type="submit" pending={pending === "brand"}>
                  {editingBrandId ? messages.adminSaveChanges : messages.adminCreateBrand}
                </PrimaryButton>
                {editingBrandId ? <SecondaryButton type="button" onClick={resetBrandForm}>{messages.adminCancel}</SecondaryButton> : null}
              </div>
            </FormStack>
          </form>
        </section>
      </div>

      <section id="products" className="mt-6 scroll-mt-24 overflow-hidden rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)]">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--ds-border)] p-5">
          <h2 className="text-xl font-bold">{messages.adminProducts}</h2>
          <label className="flex items-center gap-2 text-xs font-semibold text-[var(--ds-text-muted)]">
            {messages.adminStatusFilter}
            <select
              value={productStatusFilter}
              onChange={(event) => setProductStatusFilter(event.target.value as "Published" | "Draft" | "All")}
              className="min-h-9 rounded-lg border border-slate-300 bg-[var(--ds-surface)] px-2"
            >
              <option value="Published">{messages.catalogPublished}</option>
              <option value="Draft">{messages.catalogDraft}</option>
              <option value="All">{messages.adminAllStatuses}</option>
            </select>
          </label>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[44rem] text-start text-sm">
            <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
              <tr>
                <th className="p-4 text-start">{messages.adminName}</th>
                <th className="p-4 text-start">SKU</th>
                <th className="p-4 text-start">{messages.adminCategory}</th>
                <th className="p-4 text-start">{messages.adminBrand}</th>
                <th className="p-4 text-start">{messages.catalogPrice}</th>
                <th className="p-4 text-start">{messages.catalogPublished}</th>
              </tr>
            </thead>
            <tbody>
              {visibleProducts.map((product) => (
                <tr key={product.id} className="border-t border-slate-100">
                  <td className="p-4 font-bold"><Link className="text-blue-700" href={`/admin/catalog/products/${product.id}`}>{product.name}</Link></td>
                  <td className="p-4" dir="ltr">{product.sku}</td>
                  <td className="p-4">{product.categoryName}</td>
                  <td className="p-4">{product.brandName ?? "—"}</td>
                  <td className="p-4">{product.basePrice.toFixed(2)} EGP</td>
                  <td className="p-4">
                    <SecondaryButton type="button" onClick={() => void toggleStatus(product)} disabled={pending === product.id}>
                      {product.status === "Published" ? messages.adminHide : messages.adminPublish}
                    </SecondaryButton>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section id="media" className="mt-6 scroll-mt-24 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
        <h2 className="text-xl font-bold">{messages.adminMediaMaintenance}</h2>
        <div className="mt-4 flex flex-wrap gap-3">
          <SecondaryButton type="button" onClick={() => void auditMedia(false)} disabled={pending === "media-audit"}>
            {messages.adminAuditMedia}
          </SecondaryButton>
          <SecondaryButton type="button" onClick={() => setCleanupConfirmationOpen(true)} disabled={pending === "media-cleanup"} className="border-amber-300 text-amber-800">
            {messages.adminCleanupMedia}
          </SecondaryButton>
        </div>
        {mediaAudit ? (
          <div className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
            <Message tone={mediaAudit.missingStorageKeys.length ? "error" : "success"}>
              {messages.adminMissingImages}: {mediaAudit.missingStorageKeys.length}
            </Message>
            <Message tone={mediaAudit.orphanStorageKeys.length ? "info" : "success"}>
              {messages.adminOrphanImages}: {mediaAudit.orphanStorageKeys.length}
            </Message>
            <p className="sm:col-span-2 text-[var(--ds-text-muted)]">
              {messages.adminReferencedImages}: {mediaAudit.referencedKeyCount} · {messages.adminStoredImages}: {mediaAudit.storedFileCount}
            </p>
          </div>
        ) : null}
      </section>
      <ConfirmDialog
        open={cleanupConfirmationOpen}
        title={messages.adminCleanupMedia}
        description={messages.adminCleanupConfirm}
        confirmLabel={messages.adminCleanupMedia}
        cancelLabel={messages.adminCancel}
        pending={pending === "media-cleanup"}
        onConfirm={() => {
          setCleanupConfirmationOpen(false);
          void auditMedia(true);
        }}
        onCancel={() => setCleanupConfirmationOpen(false)}
      />
    </main>
  );
}

async function getRowVersion(productId: string): Promise<number> {
  const { getAdminProduct } = await import("@/lib/api-client");
  return (await getAdminProduct(productId)).rowVersion;
}

