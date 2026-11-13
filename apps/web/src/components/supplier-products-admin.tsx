"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  createSupplierProduct,
  getSupplier,
  getSupplierProducts,
  searchSupplierVariants,
  updateSupplierProduct,
  type Supplier,
  type SupplierProduct,
  type SupplierProductPage,
  type SupplierVariantOption,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import {
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  SideDrawer,
  TextField,
} from "@/components/ui";

const emptyPage: SupplierProductPage = { items: [], page: 1, pageSize: 25, totalCount: 0 };

export function SupplierProductsAdmin({ supplierId }: { supplierId: string }) {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const mayView = user?.permissions.includes("Purchasing.View") ?? false;
  const mayManage = user?.permissions.includes("Purchasing.ManageSuppliers") ?? false;

  const [supplier, setSupplier] = useState<Supplier | null>(null);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [data, setData] = useState<SupplierProductPage>(emptyPage);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<SupplierProduct | null>(null);
  const [creating, setCreating] = useState(false);
  const [variantQuery, setVariantQuery] = useState("");
  const [variants, setVariants] = useState<SupplierVariantOption[]>([]);
  const [selectedVariant, setSelectedVariant] = useState<SupplierVariantOption | null>(null);
  const [supplierSku, setSupplierSku] = useState("");
  const [notes, setNotes] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [rowVersion, setRowVersion] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPage(1);
    }, 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    if (status !== "authenticated" || !mayView) return;
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([
      getSupplier(supplierId),
      getSupplierProducts(supplierId, {
        search: debouncedSearch || undefined,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        page,
        pageSize: 25,
      }),
    ])
      .then(([nextSupplier, nextPage]) => {
        if (!active) return;
        setSupplier(nextSupplier);
        setData(nextPage);
      })
      .catch((requestError) => {
        if (active) setError(apiErrorMessage(requestError, messages));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [activeFilter, debouncedSearch, mayView, messages, page, status, supplierId]);

  useEffect(() => {
    if (!creating || status !== "authenticated" || !mayView) return;
    let active = true;
    const timer = window.setTimeout(() => {
      searchSupplierVariants(supplierId, variantQuery.trim())
        .then((next) => {
          if (active) setVariants(next);
        })
        .catch(() => {
          if (active) setVariants([]);
        });
    }, 220);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [creating, mayView, status, supplierId, variantQuery]);

  const pageCount = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

  async function reload() {
    const next = await getSupplierProducts(supplierId, {
      search: debouncedSearch || undefined,
      isActive: activeFilter === "all" ? undefined : activeFilter === "active",
      page,
      pageSize: 25,
    });
    setData(next);
  }

  function openCreate() {
    setCreating(true);
    setEditing(null);
    setVariantQuery("");
    setVariants([]);
    setSelectedVariant(null);
    setSupplierSku("");
    setNotes("");
    setIsActive(true);
    setRowVersion(null);
    setError(null);
    setNotice(null);
  }

  function openEdit(item: SupplierProduct) {
    setCreating(false);
    setEditing(item);
    setSelectedVariant({
      id: item.productVariantId,
      productId: item.productId,
      productName: item.productName,
      variantName: item.variantName,
      variantSku: item.variantSku,
    });
    setSupplierSku(item.supplierSku);
    setNotes(item.notes ?? "");
    setIsActive(item.isActive);
    setRowVersion(item.rowVersion);
    setError(null);
    setNotice(null);
  }

  async function save() {
    if (creating && !selectedVariant) {
      setError(messages.supplierVariantNoResults);
      return;
    }
    if (editing && rowVersion === null) {
      setError(messages.supplierProductStale);
      return;
    }
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      if (creating && selectedVariant) {
        await createSupplierProduct(supplierId, {
          productVariantId: selectedVariant.id,
          supplierSku,
          notes: notes.trim() || null,
          isActive,
        });
      } else if (editing) {
        await updateSupplierProduct(supplierId, editing.id, {
          supplierSku,
          notes: notes.trim() || null,
          isActive,
          rowVersion: rowVersion!,
        });
      }
      setNotice(messages.supplierProductSaved);
      setCreating(false);
      setEditing(null);
      await reload();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  if (status === "loading") return <LoadingCard />;
  if (!mayView) {
    return (
      <main className="mx-auto w-full max-w-5xl px-5 py-10">
        <Message tone="error">{messages.adminAccessDenied}</Message>
      </main>
    );
  }

  const drawerOpen = creating || editing !== null;

  return (
    <main className="rigx-identity-admin mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <PageIntro
            eyebrow={messages.adminGroupPurchasing}
            title={supplier ? `${messages.supplierProductsTitle} — ${supplier.companyName}` : messages.supplierProductsTitle}
            lead={messages.supplierProductsLead}
          />
        </div>
        {mayManage ? (
          <PrimaryButton type="button" onClick={openCreate}>{messages.supplierProductsAdd}</PrimaryButton>
        ) : null}
      </div>

      <div className="mb-4">
        <Link href="/admin/purchasing/suppliers" className="text-sm font-semibold text-[var(--ds-accent)] hover:text-[var(--ds-accent-strong)]">
          ← {messages.supplierBack}
        </Link>
      </div>

      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card">
        <div className="rigx-admin-card-head">
          <div>
            <h2>{messages.supplierProductsTitle}</h2>
            <p>{messages.adminIdentityTotal}: {data.totalCount}</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <label className="sr-only" htmlFor="supplier-product-search">{messages.supplierProductsSearch}</label>
            <input
              id="supplier-product-search"
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder={messages.supplierProductsSearch}
              className="rigx-admin-input min-w-64"
            />
            <select
              value={activeFilter}
              onChange={(event) => {
                setActiveFilter(event.target.value);
                setPage(1);
              }}
              className="rigx-admin-input w-auto"
              aria-label={messages.adminStatusFilter}
            >
              <option value="all">{messages.adminIdentityAllStatuses}</option>
              <option value="active">{messages.adminIdentityActiveOnly}</option>
              <option value="inactive">{messages.adminIdentityInactiveOnly}</option>
            </select>
          </div>
        </div>

        {loading ? (
          <div className="p-6"><LoadingCard /></div>
        ) : data.items.length === 0 ? (
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.supplierProductsEmpty}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[54rem] text-sm">
              <caption className="sr-only">{messages.supplierProductsTitle}</caption>
              <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
                <tr>
                  <th className="p-4 text-start">{messages.supplierProductName}</th>
                  <th className="p-4 text-start">{messages.supplierProductVariant}</th>
                  <th className="p-4 text-start">{messages.supplierProductVariantSku}</th>
                  <th className="p-4 text-start">{messages.supplierSupplierSku}</th>
                  <th className="p-4 text-start">{messages.adminIdentityStatus}</th>
                  <th className="p-4 text-start">{messages.adminActions}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--ds-border)]">
                {data.items.map((item) => (
                  <tr key={item.id} className="transition hover:bg-[var(--ds-surface-muted)]">
                    <td className="p-4 font-bold text-[var(--ds-text)]">{item.productName}</td>
                    <td className="p-4 text-[var(--ds-text-muted)]">{item.variantName}</td>
                    <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.variantSku}</td>
                    <td className="p-4 font-mono text-xs text-[var(--ds-accent-strong)]" dir="ltr">{item.supplierSku}</td>
                    <td className="p-4">
                      <span className={`rigx-admin-status ${item.isActive ? "is-published" : "is-draft"}`}>
                        {item.isActive ? messages.adminActive : messages.adminInactive}
                      </span>
                    </td>
                    <td className="p-4">
                      {mayManage ? (
                        <SecondaryButton type="button" onClick={() => openEdit(item)}>{messages.supplierProductEdit}</SecondaryButton>
                      ) : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[var(--ds-border)] p-4">
          <p className="text-sm text-[var(--ds-text-muted)]">{messages.adminPage} {data.page} / {pageCount}</p>
          <div className="flex gap-2">
            <SecondaryButton type="button" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}>
              {messages.adminPrevious}
            </SecondaryButton>
            <SecondaryButton type="button" disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}>
              {messages.adminNext}
            </SecondaryButton>
          </div>
        </div>
      </section>

      <SideDrawer
        open={drawerOpen}
        title={creating ? messages.supplierProductsAdd : messages.supplierProductEdit}
        closeLabel={messages.adminCancel}
        onClose={() => {
          if (saving) return;
          setCreating(false);
          setEditing(null);
        }}
      >
        <div className="grid gap-4">
          {creating ? (
            <>
              <TextField
                label={messages.supplierVariantSearch}
                value={variantQuery}
                onChange={(event) => setVariantQuery(event.target.value)}
                placeholder={messages.supplierVariantSearch}
              />
              <div className="max-h-64 overflow-y-auto rounded-xl border border-[var(--ds-border)]">
                {variants.length === 0 ? (
                  <p className="p-4 text-sm text-[var(--ds-text-muted)]">{messages.supplierVariantNoResults}</p>
                ) : (
                  variants.map((variant) => (
                    <button
                      key={variant.id}
                      type="button"
                      onClick={() => setSelectedVariant(variant)}
                      className={`flex w-full items-center justify-between gap-3 border-b border-[var(--ds-border)] p-3 text-start text-sm last:border-b-0 hover:bg-[var(--ds-surface-muted)] ${
                        selectedVariant?.id === variant.id ? "bg-[var(--ds-accent-soft)]" : ""
                      }`}
                    >
                      <span className="min-w-0">
                        <span className="block truncate font-semibold text-[var(--ds-text)]">{variant.productName} — {variant.variantName}</span>
                        <span className="block font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{variant.variantSku}</span>
                      </span>
                      <span className="text-xs text-[var(--ds-accent)]">{selectedVariant?.id === variant.id ? "✓" : ""}</span>
                    </button>
                  ))
                )}
              </div>
            </>
          ) : (
            <div className="rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-3 text-sm">
              <p className="font-bold text-[var(--ds-text)]">{editing?.productName}</p>
              <p className="text-xs text-[var(--ds-text-muted)]">{editing?.variantName} · <span dir="ltr">{editing?.variantSku}</span></p>
            </div>
          )}
          <TextField
            label={messages.supplierSupplierSku}
            value={supplierSku}
            onChange={(event) => setSupplierSku(event.target.value)}
            dir="ltr"
            required
          />
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.supplierProductNotes}
            <textarea
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              rows={3}
              className="rigx-admin-input"
            />
          </label>
          <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
            <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} className="h-4 w-4" />
            {messages.supplierProductActive}
          </label>
          <div className="flex flex-wrap gap-2">
            <PrimaryButton type="button" onClick={() => void save()} pending={saving} disabled={!mayManage}>
              {messages.adminSaveChanges}
            </PrimaryButton>
            <SecondaryButton type="button" onClick={() => { setCreating(false); setEditing(null); }} disabled={saving}>
              {messages.adminCancel}
            </SecondaryButton>
          </div>
        </div>
      </SideDrawer>
    </main>
  );
}
