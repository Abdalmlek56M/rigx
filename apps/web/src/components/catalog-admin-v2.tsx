"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import {
  ApiError,
  cleanupCatalogMedia,
  getAdminBrands,
  getAdminCategories,
  getAdminProduct,
  getAdminProducts,
  getCatalogMediaAudit,
  setCatalogProductStatus,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogMediaAudit,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { CatalogTaxonomyAdmin } from "@/components/catalog-taxonomy-admin";
import { RowActionMenu } from "@/components/row-action-menu";
import { ProductImage } from "@/components/product-image";
import {
  ConfirmDialog,
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  TextField,
} from "@/components/ui";
import { Icon } from "@/components/icons";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

type StatusFilter = "Published" | "Draft" | "All";

export function CatalogAdminV2() {
  const { messages, locale } = useLocale();
  const router = useRouter();
  const { status, user } = useSession();
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [productSearch, setProductSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [brandFilter, setBrandFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("Published");
  const [page, setPage] = useState(1);
  const [mediaAudit, setMediaAudit] = useState<CatalogMediaAudit | null>(null);
  const [cleanupConfirmationOpen, setCleanupConfirmationOpen] = useState(false);

  const mayManage = user?.permissions.some((permission) => permission.startsWith("Catalog.")) ?? false;
  const flatCategories = useMemo(() => flatten(categories), [categories]);

  async function load(showLoading = true) {
    if (showLoading) setLoading(true);
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
      if (showLoading) setLoading(false);
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

  useEffect(() => {
    setPage(1);
  }, [brandFilter, categoryFilter, productSearch, statusFilter]);

  const filteredProducts = useMemo(() => {
    const search = productSearch.trim().toLowerCase();
    return products.filter((product) => {
      const matchesSearch = !search ||
        product.name.toLowerCase().includes(search) ||
        product.sku.toLowerCase().includes(search);
      const matchesCategory = !categoryFilter || product.categorySlug === categoryFilter;
      const matchesBrand = !brandFilter || product.brandName === brandFilter;
      const matchesStatus = statusFilter === "All" || product.status === statusFilter;
      return matchesSearch && matchesCategory && matchesBrand && matchesStatus;
    });
  }, [brandFilter, categoryFilter, productSearch, products, statusFilter]);

  const pageSize = 8;
  const pageCount = Math.max(1, Math.ceil(filteredProducts.length / pageSize));
  const safePage = Math.min(page, pageCount);
  const visibleProducts = filteredProducts.slice((safePage - 1) * pageSize, safePage * pageSize);

  async function toggleStatus(product: CatalogProductSummary) {
    setPending(product.id);
    setError(null);
    try {
      const detail = await getAdminProduct(product.id);
      const result = await setCatalogProductStatus(
        product.id,
        product.status === "Published" ? "Draft" : "Published",
        detail.rowVersion,
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

  if (status === "loading" || loading) return <LoadingCard />;
  if (status === "authenticated" && !mayManage) {
    return <main className="mx-auto max-w-3xl px-5 py-16"><Message tone="error">{messages.adminCatalogForbidden}</Message></main>;
  }

  return (
    <main className="mx-auto w-full max-w-[82rem] px-4 py-7 sm:px-6 lg:px-8 lg:py-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <PageIntro eyebrow={messages.adminCatalogEyebrow} title={messages.adminCatalogTitle} lead={messages.adminCatalogLead} />
        <Link
          href="/admin/catalog/products/new"
          className="inline-flex min-h-11 items-center rounded-xl bg-[var(--ds-accent)] px-5 text-sm font-bold text-[var(--ds-on-accent)] shadow-sm hover:bg-[var(--ds-accent-strong)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)]"
        >
          + {messages.adminNewProduct}
        </Link>
      </div>
      {notice ? <div className="mb-5"><Message tone="success">{notice}</Message></div> : null}
      {error ? <div className="mb-5"><Message tone="error">{error}</Message></div> : null}

      <CatalogTaxonomyAdmin
        categories={categories}
        brands={brands}
        products={products}
        onChanged={() => load(false)}
        onNotice={setNotice}
        onError={setError}
      />

      <section id="products" className="mt-6 scroll-mt-24 overflow-hidden rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)]">
        <div className="border-b border-[var(--ds-border)] p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.16em] text-[var(--ds-accent)]">RIGX ADMIN</p>
              <h2 className="mt-1 text-2xl font-black text-[var(--ds-text)]">{messages.adminProducts}</h2>
            </div>
            <p className="text-sm font-semibold text-[var(--ds-text-muted)]">{filteredProducts.length} {messages.adminProductResults}</p>
          </div>
          <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <TextField
              label={messages.adminSearchProducts}
              type="search"
              value={productSearch}
              onChange={(event) => setProductSearch(event.target.value)}
              placeholder={messages.searchPlaceholder}
            />
            <label className="text-sm font-semibold text-[var(--ds-text)]">
              {messages.adminCategory}
              <select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-[var(--ds-text)]">
                <option value="">{messages.adminAllCategoriesFilter}</option>
                {flatCategories.map((category) => <option key={category.id} value={category.slug}>{category.name}</option>)}
              </select>
            </label>
            <label className="text-sm font-semibold text-[var(--ds-text)]">
              {messages.adminBrand}
              <select value={brandFilter} onChange={(event) => setBrandFilter(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-[var(--ds-text)]">
                <option value="">{messages.adminAllBrandsFilter}</option>
                {brands.map((brand) => <option key={brand.id} value={brand.name}>{brand.name}</option>)}
              </select>
            </label>
            <label className="text-sm font-semibold text-[var(--ds-text)]">
              {messages.adminStatusFilter}
              <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as StatusFilter)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-[var(--ds-text)]">
                <option value="Published">{messages.catalogPublished}</option>
                <option value="Draft">{messages.catalogDraft}</option>
                <option value="All">{messages.adminAllStatuses}</option>
              </select>
            </label>
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full min-w-[54rem] text-start text-sm">
            <caption className="sr-only">{messages.adminProducts}</caption>
            <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
              <tr>
                <th className="p-4 text-start">{messages.adminImage}</th>
                <th className="p-4 text-start">{messages.adminName}</th>
                <th className="p-4 text-start">SKU</th>
                <th className="p-4 text-start">{messages.adminCategory}</th>
                <th className="p-4 text-start">{messages.adminBrand}</th>
                <th className="p-4 text-start">{messages.catalogPrice}</th>
                <th className="p-4 text-start">{messages.catalogPublished}</th>
                <th className="p-4 text-start">{messages.adminActions}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--ds-border)]">
              {visibleProducts.map((product) => (
                <tr key={product.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                  <td className="p-4">
                    <span className="rigx-admin-thumb">
                      <ProductImage product={product} alt={product.name} compact />
                    </span>
                  </td>
                  <td className="max-w-64 p-4">
                    <Link href={`/admin/catalog/products/${product.id}`} className="font-bold text-[var(--ds-text)] hover:text-[var(--ds-accent)]">{product.name}</Link>
                  </td>
                  <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{product.sku}</td>
                  <td className="p-4 text-[var(--ds-text-muted)]">{product.categoryName}</td>
                  <td className="p-4 text-[var(--ds-text-muted)]">{product.brandName ?? "—"}</td>
                  <td className="whitespace-nowrap p-4 font-bold text-[var(--ds-text)]">{formatCatalogPrice(product.basePrice)} {currencyLabel(locale)}</td>
                  <td className="p-4">
                    <span className={`rigx-admin-status ${product.status === "Published" ? "is-published" : "is-draft"}`}>
                      {product.status === "Published" ? messages.catalogPublished : messages.catalogDraft}
                    </span>
                  </td>
                  <td className="p-4">
                    <RowActionMenu
                      label={messages.adminActions}
                      actions={[
                        {
                          key: "edit",
                          label: messages.adminEdit,
                          icon: "settings",
                          href: `/admin/catalog/products/${product.id}`,
                        },
                        {
                          key: "preview",
                          label: messages.catalogProductDetails,
                          icon: "eye",
                          href: `/admin/catalog/products/${product.id}/preview`,
                          target: "_blank",
                        },
                        {
                          key: "status",
                          label: product.status === "Published" ? messages.adminHide : messages.adminPublish,
                          icon: product.status === "Published" ? "eyeOff" : "check",
                          onSelect: () => void toggleStatus(product),
                          disabled: pending === product.id,
                        },
                      ]}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {visibleProducts.length === 0 ? <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.adminNoResults}</p> : null}
        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--ds-border)] p-4">
          <p className="text-sm text-[var(--ds-text-muted)]">{messages.adminPage} {safePage} / {pageCount}</p>
          <div className="flex gap-2">
            <SecondaryButton type="button" disabled={safePage <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>{messages.adminPrevious}</SecondaryButton>
            <SecondaryButton type="button" disabled={safePage >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>{messages.adminNext}</SecondaryButton>
          </div>
        </div>
      </section>

      <section id="media" className="mt-6 scroll-mt-24 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-[var(--ds-accent)]">RIGX ADMIN</p>
            <h2 className="mt-1 text-2xl font-black text-[var(--ds-text)]">{messages.adminMediaMaintenance}</h2>
          </div>
          <div className="flex flex-wrap gap-2">
            <SecondaryButton type="button" onClick={() => void auditMedia(false)} disabled={pending === "media-audit"}>
              <Icon name="search" className="h-4 w-4" /> {messages.adminAuditMedia}
            </SecondaryButton>
            <SecondaryButton type="button" onClick={() => setCleanupConfirmationOpen(true)} disabled={pending === "media-cleanup"} className="border-[var(--ds-danger)] text-[var(--ds-danger)]">
              <Icon name="trash" className="h-4 w-4" /> {messages.adminCleanupMedia}
            </SecondaryButton>
          </div>
        </div>
        {mediaAudit ? (
          <div className="mt-5 grid gap-3 text-sm sm:grid-cols-2">
            <Message tone={mediaAudit.missingStorageKeys.length ? "error" : "success"}>{messages.adminMissingImages}: {mediaAudit.missingStorageKeys.length}</Message>
            <Message tone={mediaAudit.orphanStorageKeys.length ? "info" : "success"}>{messages.adminOrphanImages}: {mediaAudit.orphanStorageKeys.length}</Message>
            <p className="text-[var(--ds-text-muted)] sm:col-span-2">{messages.adminReferencedImages}: {mediaAudit.referencedKeyCount} · {messages.adminStoredImages}: {mediaAudit.storedFileCount}</p>
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

function flatten(categories: CatalogCategory[], depth = 0): Array<CatalogCategory & { depth: number }> {
  return categories.flatMap((category) => [
    { ...category, depth },
    ...flatten(category.children, depth + 1),
  ]);
}
