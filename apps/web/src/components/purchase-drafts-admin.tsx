"use client";

import { useEffect, useState } from "react";
import {
  cancelPurchaseDraft,
  createPurchaseDraft,
  getAdminProduct,
  getAdminProducts,
  getPurchaseDrafts,
  getSuppliers,
  updatePurchaseDraft,
  type CatalogProductSummary,
  type PurchaseDraft,
  type PurchaseDraftInput,
  type PurchaseSourceType,
  type Supplier,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

type VariantOption = { id: string; name: string; sku: string };
type ItemRow = { productId: string; variantId: string; quantity: string; unitPrice: string; itemName: string; externalCode: string };

const sources: Array<{ value: PurchaseSourceType; labelKey: "draftsRegisteredSupplier" | "draftsMarketplaceSource" | "draftsIndividualSeller" | "draftsOtherSource" }> = [
  { value: "RegisteredSupplier", labelKey: "draftsRegisteredSupplier" },
  { value: "Marketplace", labelKey: "draftsMarketplaceSource" },
  { value: "IndividualSeller", labelKey: "draftsIndividualSeller" },
  { value: "Other", labelKey: "draftsOtherSource" },
];

export function PurchaseDraftsAdmin() {
  const { messages } = useLocale();
  const [drafts, setDrafts] = useState<PurchaseDraft[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [variants, setVariants] = useState<Record<string, VariantOption[]>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<PurchaseDraft | null>(null);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    sourceType: "Marketplace" as PurchaseSourceType,
    supplierId: "",
    marketplaceName: "",
    externalSellerName: "",
    externalSellerContact: "",
    listingUrl: "",
    purchaseDate: "",
    notes: "",
  });
  const [items, setItems] = useState<ItemRow[]>([{ productId: "", variantId: "", quantity: "1", unitPrice: "", itemName: "", externalCode: "" }]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [page, productList, supplierPage] = await Promise.all([
        getPurchaseDrafts({ pageSize: 100 }),
        getAdminProducts(),
        getSuppliers({ pageSize: 100 }).catch(() => null),
      ]);
      setDrafts(page.items);
      setProducts(productList);
      setSuppliers(supplierPage?.items ?? []);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function ensureVariants(productId: string) {
    if (!productId || variants[productId]) return;
    try {
      const product = await getAdminProduct(productId);
      setVariants((current) => ({
        ...current,
        [productId]: product.variants.filter((variant) => variant.isActive).map((variant) => ({ id: variant.id, name: variant.name, sku: variant.sku })),
      }));
    } catch {
      setVariants((current) => ({ ...current, [productId]: [] }));
    }
  }

  function openNew() {
    setEditing(null);
    setForm({
      sourceType: "Marketplace",
      supplierId: "",
      marketplaceName: "",
      externalSellerName: "",
      externalSellerContact: "",
      listingUrl: "",
      purchaseDate: new Date().toISOString().slice(0, 10),
      notes: "",
    });
    setItems([{ productId: "", variantId: "", quantity: "1", unitPrice: "", itemName: "", externalCode: "" }]);
    setOpen(true);
    setNotice(null);
    setError(null);
  }

  function openEdit(draft: PurchaseDraft) {
    setEditing(draft);
    setForm({
      sourceType: draft.sourceType,
      supplierId: draft.supplierId ?? "",
      marketplaceName: draft.marketplaceName ?? "",
      externalSellerName: draft.externalSellerName ?? "",
      externalSellerContact: draft.externalSellerContact ?? "",
      listingUrl: draft.listingUrl ?? "",
      purchaseDate: draft.purchaseDate ?? "",
      notes: draft.notes ?? "",
    });
    setItems(draft.items.map((item) => ({
      productId: item.productId,
      variantId: item.variantId ?? "",
      quantity: String(item.quantity),
      unitPrice: String(item.unitPrice),
      itemName: item.productId === "00000000-0000-0000-0000-000000000000" ? item.itemName : "",
      externalCode: item.externalCode ?? "",
    })));
    draft.items.forEach((item) => void ensureVariants(item.productId));
    setOpen(true);
    setNotice(null);
    setError(null);
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const input: PurchaseDraftInput = {
      sourceType: form.sourceType,
      supplierId: form.sourceType === "RegisteredSupplier" && form.supplierId ? form.supplierId : null,
      marketplaceName: form.marketplaceName.trim() || null,
      externalSellerName: form.externalSellerName.trim() || null,
      externalSellerContact: form.externalSellerContact.trim() || null,
      listingUrl: form.listingUrl.trim() || null,
      purchaseDate: form.purchaseDate || null,
      notes: form.notes.trim() || null,
      items: items
        .filter((item) => (item.productId || item.itemName.trim()) && Number(item.quantity) > 0 && Number(item.unitPrice) > 0)
        .map((item) => ({
          productId: item.productId || "00000000-0000-0000-0000-000000000000",
          variantId: item.variantId || null,
          quantity: Number(item.quantity),
          unitPrice: Number(item.unitPrice),
          itemName: item.itemName.trim() || null,
          externalCode: item.externalCode.trim() || null,
        })),
      rowVersion: editing?.rowVersion,
    };
    try {
      if (editing) await updatePurchaseDraft(editing.id, input);
      else await createPurchaseDraft(input);
      setNotice(editing ? messages.draftsUpdated : messages.draftsCreate);
      setOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function cancel(draft: PurchaseDraft) {
    setError(null);
    try {
      await cancelPurchaseDraft(draft.id, draft.rowVersion);
      setNotice(messages.adminSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  const total = items.reduce((sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0), 0);

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-6xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / PURCHASING" title={messages.draftsTitle} lead={messages.draftsLead} />
        <PrimaryButton type="button" onClick={openNew}>{messages.draftsNew}</PrimaryButton>
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : drafts.length === 0 ? (
        <Message tone="info">{messages.draftsEmpty}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {drafts.map((draft) => (
            <article key={draft.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <p className="font-bold text-[var(--ds-text)]">
                  {draft.supplierCompanyName ?? draft.externalSellerName ?? draft.marketplaceName ?? draft.sourceType}
                </p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">
                  {draft.sourceType} · {draft.items.length} items · {draft.totalAmount.toLocaleString()} EGP
                </p>
                <p className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{draft.purchaseDate ?? "-"}</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {draft.status === "Cancelled" ? messages.draftsCancelled : messages.draftsStatusDraft}
                </span>
                {draft.status === "Draft" ? (
                  <>
                    <SecondaryButton type="button" onClick={() => openEdit(draft)}>{messages.adminEdit}</SecondaryButton>
                    <SecondaryButton type="button" onClick={() => void cancel(draft)}>{messages.draftsCancel}</SecondaryButton>
                  </>
                ) : null}
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={open} title={editing ? messages.adminEdit : messages.draftsNew} closeLabel={messages.adminCancel} onClose={() => setOpen(false)}>
        <div className="grid gap-4">
          <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
            {messages.draftsSource}
            <select
              value={form.sourceType}
              onChange={(event) => setForm((current) => ({ ...current, sourceType: event.target.value as PurchaseSourceType }))}
              className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
            >
              {sources.map((source) => <option key={source.value} value={source.value}>{messages[source.labelKey]}</option>)}
            </select>
          </label>
          {form.sourceType === "RegisteredSupplier" ? (
            <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
              {messages.draftsSupplier}
              <select
                value={form.supplierId}
                onChange={(event) => setForm((current) => ({ ...current, supplierId: event.target.value }))}
                className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
              >
                <option value="">-</option>
                {suppliers.map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.companyName}</option>)}
              </select>
            </label>
          ) : null}
          {form.sourceType === "Marketplace" || form.sourceType === "Other" ? (
            <TextField label={messages.draftsMarketplace} value={form.marketplaceName} onChange={(event) => setForm((current) => ({ ...current, marketplaceName: event.target.value }))} />
          ) : null}
          {form.sourceType !== "RegisteredSupplier" ? (
            <>
              <TextField label={messages.draftsSellerName} value={form.externalSellerName} onChange={(event) => setForm((current) => ({ ...current, externalSellerName: event.target.value }))} />
              <TextField label={messages.draftsSellerContact} value={form.externalSellerContact} onChange={(event) => setForm((current) => ({ ...current, externalSellerContact: event.target.value }))} dir="ltr" />
            </>
          ) : null}
          <TextField label={messages.draftsListingUrl} value={form.listingUrl} onChange={(event) => setForm((current) => ({ ...current, listingUrl: event.target.value }))} dir="ltr" />
          <TextField label={messages.draftsDate} value={form.purchaseDate} onChange={(event) => setForm((current) => ({ ...current, purchaseDate: event.target.value }))} dir="ltr" />
          <TextField label={messages.draftsNotes} value={form.notes} onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))} />

          <div className="grid gap-2">
            <div className="flex items-center justify-between">
              <p className="text-sm font-bold text-[var(--ds-text)]">{messages.draftsItems}</p>
              <SecondaryButton type="button" onClick={() => setItems((current) => [...current, { productId: "", variantId: "", quantity: "1", unitPrice: "", itemName: "", externalCode: "" }])}>{messages.draftsAddItem}</SecondaryButton>
            </div>
            {items.map((item, index) => (
              <div key={index} className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3">
                <select
                  value={item.productId}
                  onChange={(event) => {
                    const productId = event.target.value;
                    void ensureVariants(productId);
                    setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, productId, variantId: "" } : row));
                  }}
                  className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                >
                  <option value="">{messages.draftsProduct}</option>
                  {products.map((product) => <option key={product.id} value={product.id}>{product.name} · {product.sku}</option>)}
                </select>
                {!item.productId ? (
                  <div className="grid gap-2 sm:grid-cols-2">
                    <input
                      value={item.itemName}
                      onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, itemName: event.target.value } : row))}
                      placeholder={messages.draftsManualName}
                      className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                    />
                    <input
                      value={item.externalCode}
                      onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, externalCode: event.target.value } : row))}
                      placeholder={messages.draftsExternalCode}
                      dir="ltr"
                      className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                    />
                  </div>
                ) : null}
                <div className="grid gap-2 sm:grid-cols-3">
                  <select
                    value={item.variantId}
                    onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, variantId: event.target.value } : row))}
                    className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                    disabled={!item.productId}
                  >
                    <option value="">{messages.draftsVariant}</option>
                    {(variants[item.productId] ?? []).map((variant) => <option key={variant.id} value={variant.id}>{variant.name} · {variant.sku}</option>)}
                  </select>
                  <input
                    type="number" min="1" dir="ltr" value={item.quantity}
                    onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, quantity: event.target.value } : row))}
                    placeholder={messages.draftsQuantity}
                    className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                  />
                  <input
                    type="number" min="0.01" step="0.01" dir="ltr" value={item.unitPrice}
                    onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, unitPrice: event.target.value } : row))}
                    placeholder={messages.draftsUnitPrice}
                    className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                  />
                </div>
                <div className="flex items-center justify-between text-xs">
                  <span dir="ltr" className="text-[var(--ds-text-muted)]">
                    {messages.draftsLineTotal}: {((Number(item.quantity) || 0) * (Number(item.unitPrice) || 0)).toLocaleString()}
                  </span>
                  <SecondaryButton type="button" onClick={() => setItems((current) => current.filter((_, rowIndex) => rowIndex !== index))}>{messages.adminDelete}</SecondaryButton>
                </div>
              </div>
            ))}
            <p className="text-sm font-bold text-[var(--ds-text)]">{messages.draftsTotal}: <span dir="ltr">{total.toLocaleString()} EGP</span></p>
          </div>
          <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.draftsSave}</PrimaryButton>
        </div>
      </SideDrawer>
    </main>
  );
}
