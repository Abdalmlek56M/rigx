"use client";

import { useEffect, useState } from "react";
import {
  cancelOffer,
  createOffer,
  getAdminOffers,
  getAdminProducts,
  setOfferPublished,
  updateOffer,
  type CatalogProductSummary,
  type Offer,
  type OfferInput,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";

type ItemRow = { productId: string; offerPrice: string };

function toDateInput(value: string | null): string {
  return value ? value.slice(0, 10) : "";
}

export function ContentOffersAdmin() {
  const { messages } = useLocale();
  const [offers, setOffers] = useState<Offer[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [editing, setEditing] = useState<Offer | null>(null);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({
    titleAr: "",
    titleEn: "",
    descriptionAr: "",
    descriptionEn: "",
    startsAt: "",
    endsAt: "",
    priority: "0",
  });
  const [items, setItems] = useState<ItemRow[]>([]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [offerList, productList] = await Promise.all([getAdminOffers(), getAdminProducts()]);
      setOffers(offerList);
      setProducts(productList.filter((product) => product.status === "Published"));
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

  function openNew() {
    setEditing(null);
    setForm({ titleAr: "", titleEn: "", descriptionAr: "", descriptionEn: "", startsAt: toDateInput(new Date().toISOString()), endsAt: "", priority: "0" });
    setItems([{ productId: "", offerPrice: "" }]);
    setOpen(true);
    setNotice(null);
    setError(null);
  }

  function openEdit(offer: Offer) {
    setEditing(offer);
    setForm({
      titleAr: offer.titleAr,
      titleEn: offer.titleEn,
      descriptionAr: offer.descriptionAr ?? "",
      descriptionEn: offer.descriptionEn ?? "",
      startsAt: toDateInput(offer.startsAtUtc),
      endsAt: toDateInput(offer.endsAtUtc),
      priority: String(offer.priority),
    });
    setItems(offer.items.map((item) => ({ productId: item.productId, offerPrice: String(item.offerPrice) })));
    setOpen(true);
    setNotice(null);
    setError(null);
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);
    const starts = form.startsAt ? new Date(`${form.startsAt}T00:00:00Z`).toISOString() : new Date().toISOString();
    const ends = form.endsAt ? new Date(`${form.endsAt}T23:59:59Z`).toISOString() : null;
    const input: OfferInput = {
      titleAr: form.titleAr,
      titleEn: form.titleEn,
      descriptionAr: form.descriptionAr.trim() || null,
      descriptionEn: form.descriptionEn.trim() || null,
      startsAtUtc: starts,
      endsAtUtc: ends,
      priority: Number(form.priority) || 0,
      items: items
        .filter((item) => item.productId && Number(item.offerPrice) > 0)
        .map((item) => ({ productId: item.productId, offerPrice: Number(item.offerPrice) })),
      rowVersion: editing?.rowVersion,
    };
    try {
      if (editing) await updateOffer(editing.id, input);
      else await createOffer(input);
      setNotice(messages.adminSaved);
      setOpen(false);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSaving(false);
    }
  }

  async function toggle(offer: Offer) {
    setError(null);
    try {
      await setOfferPublished(offer.id, offer.status !== "Published", offer.rowVersion);
      setNotice(offer.status === "Published" ? messages.adminUnpublished : messages.adminPublished);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  async function cancel(offer: Offer) {
    setError(null);
    try {
      await cancelOffer(offer.id, offer.rowVersion);
      setNotice(messages.adminSaved);
      await load();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    }
  }

  const statusLabel = (status: string) =>
    status === "Published" ? messages.contentStatusPublished : status === "Cancelled" ? messages.draftsCancelled : messages.contentStatusDraft;

  return (
    <main className="rigx-identity-admin mx-auto grid w-full max-w-6xl gap-6 px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <PageIntro eyebrow="RIGX / OFFERS" title={messages.offersTitle} lead={messages.offersLead} />
        <PrimaryButton type="button" onClick={openNew}>{messages.adminMediaUpload}</PrimaryButton>
      </div>
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}

      {loading ? <LoadingCard /> : offers.length === 0 ? (
        <Message tone="info">{messages.offersEmpty}</Message>
      ) : (
        <section className="rigx-admin-card divide-y divide-[var(--ds-border)]">
          {offers.map((offer) => (
            <article key={offer.id} className="flex flex-wrap items-center justify-between gap-3 p-4">
              <div className="min-w-0">
                <p className="font-bold text-[var(--ds-text)]">{offer.titleAr}</p>
                <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{offer.titleEn} · {offer.items.length} items</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold text-[var(--ds-text-muted)]">
                  {statusLabel(offer.status)}
                </span>
                <SecondaryButton type="button" onClick={() => openEdit(offer)}>{messages.adminEdit}</SecondaryButton>
                {offer.status !== "Cancelled" ? (
                  <SecondaryButton type="button" onClick={() => void toggle(offer)}>
                    {offer.status === "Published" ? messages.adminUnpublish : messages.adminPublish}
                  </SecondaryButton>
                ) : null}
                {offer.status !== "Cancelled" ? (
                  <SecondaryButton type="button" onClick={() => void cancel(offer)}>{messages.draftsCancel}</SecondaryButton>
                ) : null}
              </div>
            </article>
          ))}
        </section>
      )}

      <SideDrawer open={open} title={editing ? messages.adminEdit : messages.offersTitle} closeLabel={messages.adminCancel} onClose={() => setOpen(false)}>
        <div className="grid gap-4">
          <TextField label={messages.adminArticleTitleAr} value={form.titleAr} onChange={(event) => setForm((current) => ({ ...current, titleAr: event.target.value }))} required />
          <TextField label={messages.adminArticleTitleEn} value={form.titleEn} onChange={(event) => setForm((current) => ({ ...current, titleEn: event.target.value }))} dir="ltr" required />
          <TextField label={messages.adminArticleSummaryAr} value={form.descriptionAr} onChange={(event) => setForm((current) => ({ ...current, descriptionAr: event.target.value }))} />
          <TextField label={messages.adminArticleSummaryEn} value={form.descriptionEn} onChange={(event) => setForm((current) => ({ ...current, descriptionEn: event.target.value }))} dir="ltr" />
          <div className="grid gap-4 sm:grid-cols-2">
            <TextField label={messages.auditFrom} value={form.startsAt} onChange={(event) => setForm((current) => ({ ...current, startsAt: event.target.value }))} dir="ltr" />
            <TextField label={messages.auditTo} value={form.endsAt} onChange={(event) => setForm((current) => ({ ...current, endsAt: event.target.value }))} dir="ltr" />
          </div>
          <TextField label={messages.adminSortOrder} value={form.priority} onChange={(event) => setForm((current) => ({ ...current, priority: event.target.value }))} inputMode="numeric" dir="ltr" />
          <div className="grid gap-2">
            <div className="flex items-center justify-between">
              <p className="text-sm font-bold text-[var(--ds-text)]">{messages.draftsItems}</p>
              <SecondaryButton type="button" onClick={() => setItems((current) => [...current, { productId: "", offerPrice: "" }])}>{messages.draftsAddItem}</SecondaryButton>
            </div>
            {items.map((item, index) => {
              const product = products.find((entry) => entry.id === item.productId);
              return (
                <div key={index} className="grid gap-2 rounded-xl border border-[var(--ds-border)] p-3 sm:grid-cols-[1fr_8rem_auto]">
                  <select
                    value={item.productId}
                    onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, productId: event.target.value } : row))}
                    className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                  >
                    <option value="">{messages.draftsProduct}</option>
                    {products.map((entry) => <option key={entry.id} value={entry.id}>{entry.name}</option>)}
                  </select>
                  <input
                    type="number"
                    min="0.01"
                    step="0.01"
                    value={item.offerPrice}
                    onChange={(event) => setItems((current) => current.map((row, rowIndex) => rowIndex === index ? { ...row, offerPrice: event.target.value } : row))}
                    placeholder={messages.draftsUnitPrice}
                    dir="ltr"
                    className="min-h-11 rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3 text-sm"
                  />
                  <SecondaryButton type="button" onClick={() => setItems((current) => current.filter((_, rowIndex) => rowIndex !== index))}>{messages.adminDelete}</SecondaryButton>
                  {product ? <p className="text-xs text-[var(--ds-text-muted)] sm:col-span-3" dir="ltr">{messages.offersWas}: {product.basePrice}</p> : null}
                </div>
              );
            })}
          </div>
          <PrimaryButton type="button" onClick={() => void save()} pending={saving}>{messages.adminSaveChanges}</PrimaryButton>
        </div>
      </SideDrawer>
    </main>
  );
}
